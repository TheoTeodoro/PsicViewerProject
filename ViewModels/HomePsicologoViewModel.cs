using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MauiApp1.Services;
using MauiApp1.Views;

namespace MauiApp1.ViewModels
{
	public partial class HomePsicologoViewModel : ObservableObject
	{
		private readonly SessaoUsuario _sessao;
		private readonly IServiceProvider _serviceProvider;
		private readonly ChatConnectionService _chat;
		private readonly VinculoApiService _vinculo;
		private readonly NotificacaoService _notificacoes;
		private readonly QuestionarioApiService _questionarios;

		[ObservableProperty]
		private string nomeUsuario = string.Empty;

		[ObservableProperty]
		private int pacientesAtivos;

		[ObservableProperty]
		private int questionariosAtivos;

		[ObservableProperty]
		private int respostasPendentes;

		[ObservableProperty]
		private bool temNotificacao;

		[ObservableProperty]
		private int notificacoesNaoLidas;

		// Começa recolhido — só o título "Sumário Clínico" aparece, os
		// números ficam escondidos até o psicólogo tocar pra expandir.
		[ObservableProperty]
		private bool sumarioExpandido;

		public string SetaSumario => SumarioExpandido ? "▲" : "▼";

		partial void OnSumarioExpandidoChanged(bool value) => OnPropertyChanged(nameof(SetaSumario));

		public string FotoExibida => string.IsNullOrEmpty(_sessao.FotoUrl)
			? "avatar_placeholder.jpg"
			: $"{ApiConfig.ServidorBaseUrl}{_sessao.FotoUrl}";

		public HomePsicologoViewModel(SessaoUsuario sessao, IServiceProvider serviceProvider, ChatConnectionService chat, VinculoApiService vinculo, NotificacaoService notificacoes, QuestionarioApiService questionarios)
		{
			_sessao = sessao;
			_serviceProvider = serviceProvider;
			_chat = chat;
			_vinculo = vinculo;
			_notificacoes = notificacoes;
			_questionarios = questionarios;
			NomeUsuario = string.IsNullOrWhiteSpace(_sessao.Nome)
				? "Psicólogo(a)"
				: _sessao.Nome.Split(' ')[0];
		}

		public void AtualizarFoto() => OnPropertyChanged(nameof(FotoExibida));

		[RelayCommand]
		private void AlternarSumario() => SumarioExpandido = !SumarioExpandido;

		/// <summary>Chamado no OnAppearing — busca os números reais do
		/// Sumário Clínico. RespostasPendentes conta quantas perguntas
		/// ativas ainda não foram respondidas HOJE, somando todos os
		/// pacientes vinculados.</summary>
		public async Task CarregarSumarioAsync()
		{
			try
			{
				var vinculos = await _vinculo.ListarPorPsicologoAsync(_sessao.UsuarioId);
				PacientesAtivos = vinculos.Count(v => v.Status == "Aceito");

				QuestionariosAtivos = await _questionarios.ObterQuestionariosEmUsoAsync(_sessao.UsuarioId);
				RespostasPendentes = await _questionarios.ObterPerguntasPendentesHojeAsync(_sessao.UsuarioId);
			}
			catch
			{
				// Sumário não deve travar a Home se a API estiver fora.
			}
		}

		public async Task VerificarNotificacoesAsync()
		{
			try
			{
				var todas = await _notificacoes.ObterNotificacoesAsync();
				NotificacoesNaoLidas = todas.Count(i => i.NaoLida);
				TemNotificacao = todas.Any(i => i.Tipo == TipoNotificacao.SolicitacaoVinculo && i.NaoLida);

				var vinculos = await _vinculo.ListarPorPsicologoAsync(_sessao.UsuarioId);
				var aceitosNovos = vinculos.Where(v =>
					v.Status == "Aceito" && v.Origem == "Psicologo" && !v.AceitoVisualizado);
				foreach (var v in aceitosNovos)
				{
					await _vinculo.MarcarAceitoVisualizadoAsync(v.Id);
					await Application.Current!.MainPage!.DisplayAlert(
						"Vínculo aceito! 🎉", $"{v.ContatoNome} aceitou seu convite de vínculo.", "OK");
				}
			}
			catch
			{
				// Notificação não deve travar a Home se a API estiver fora.
			}
		}

		[RelayCommand]
		private async Task AbrirNotificacaoAsync()
		{
			var page = _serviceProvider.GetRequiredService<NotificacoesPage>();
			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}

		[RelayCommand]
		private async Task SairAsync()
		{
			await _chat.DesconectarAsync();
			_sessao.EncerrarSessao();

			var loginPage = _serviceProvider.GetRequiredService<Views.LoginPage>();
			Application.Current!.MainPage = new NavigationPage(loginPage);
		}

		[RelayCommand]
		private async Task AbrirPacientesAsync()
		{
			var page = _serviceProvider.GetRequiredService<PacientesPage>();
			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}

		[RelayCommand]
		private async Task AbrirQuestionariosAsync()
		{
			var page = _serviceProvider.GetRequiredService<QuestionariosPage>();
			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}

		[RelayCommand]
		private async Task AbrirRelatoriosAsync()
		{
			var page = _serviceProvider.GetRequiredService<RelatoriosPage>();
			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}

		[RelayCommand]
		private async Task AbrirChatAsync()
		{
			var page = _serviceProvider.GetRequiredService<ChatListPage>();
			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}

		[RelayCommand]
		private async Task AbrirPerfilAsync()
		{
			var page = _serviceProvider.GetRequiredService<PerfilPage>();
			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}
	}
}