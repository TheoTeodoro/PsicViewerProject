using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MauiApp1.Services;
using MauiApp1.Views;

namespace MauiApp1.ViewModels
{
	public partial class HomePacienteViewModel : ObservableObject
	{
		private readonly SessaoUsuario _sessao;
		private readonly IServiceProvider _serviceProvider;
		private readonly ChatConnectionService _chat;
		private readonly VinculoApiService _vinculo;
		private readonly NotificacaoService _notificacoes;
		[ObservableProperty]
		private string nomeUsuario = string.Empty;
		[ObservableProperty]
		private bool jaRegistrouHumorHoje = false;
		[ObservableProperty]
		private bool temNotificacao;
		[ObservableProperty]
		private int notificacoesNaoLidas;
		public string FotoExibida => string.IsNullOrEmpty(_sessao.FotoUrl)
			? "avatar_placeholder.jpg"
			: $"{ApiConfig.ServidorBaseUrl}{_sessao.FotoUrl}";
		public HomePacienteViewModel(SessaoUsuario sessao, IServiceProvider serviceProvider, ChatConnectionService chat, VinculoApiService vinculo, NotificacaoService notificacoes)
		{
			_sessao = sessao;
			_serviceProvider = serviceProvider;
			_chat = chat;
			_vinculo = vinculo;
			_notificacoes = notificacoes;
			NomeUsuario = string.IsNullOrWhiteSpace(_sessao.Nome)
				? "Paciente"
				: _sessao.Nome.Split(' ')[0];
		}
		public void AtualizarFoto() => OnPropertyChanged(nameof(FotoExibida));
		/// <summary>Chamado no OnAppearing da Home. Liga o sino se tiver
		/// convite de psicólogo esperando resposta, e mostra um aviso
		/// rápido se algum pedido que o paciente mandou acabou de ser
		/// aceito (só uma vez — controlado pela SessaoUsuario).</summary>
		public async Task VerificarNotificacoesAsync()
		{
			try
			{
				// Contagem do sino: TUDO (vínculo + mensagens de chat não
				// lidas) — antes só contava vínculo, por isso não subia
				// quando chegava mensagem nova.
				var todas = await _notificacoes.ObterNotificacoesAsync();
				NotificacoesNaoLidas = todas.Count(i => i.NaoLida);
				TemNotificacao = todas.Any(i => i.Tipo == TipoNotificacao.SolicitacaoVinculo && i.NaoLida);

				// Aviso rápido de "aceito" continua só sobre vínculo.
				var vinculos = await _vinculo.ListarPorPacienteAsync(_sessao.UsuarioId);
				var aceitosNovos = vinculos.Where(v =>
					v.Status == "Aceito" && v.Origem == "Paciente" && !v.AceitoVisualizado);
				foreach (var v in aceitosNovos)
				{
					await _vinculo.MarcarAceitoVisualizadoAsync(v.Id);
					await Application.Current!.MainPage!.DisplayAlert(
						"Vínculo aceito! 🎉", $"{v.ContatoNome} aceitou seu pedido de vínculo.", "OK");
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
		private async Task RegistrarHumorAsync()
			=> await Application.Current!.MainPage!.DisplayAlert("Em breve", "Registro de humor ainda não implementado.", "OK");

		/// <summary>Abre a tela de Questionários já na aba "Histórico" —
		/// reaproveita o que já existe lá (respostas de dias anteriores,
		/// agrupadas por dia, só leitura) em vez de duplicar em outro
		/// lugar. Antes só mostrava "Em breve".</summary>
		[RelayCommand]
		private async Task AbrirHistoricoAsync()
		{
			var page = _serviceProvider.GetRequiredService<QuestionariosPacientePage>();
			if (page.BindingContext is QuestionariosPacienteViewModel vm)
				await vm.FiltrarHistoricoCommand.ExecuteAsync(null);

			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}
		[RelayCommand]
		private async Task AbrirQuestionariosAsync()
		{
			var page = _serviceProvider.GetRequiredService<QuestionariosPacientePage>();
			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}
		[RelayCommand]
		private async Task AbrirChatAsync()
		{
			var page = _serviceProvider.GetRequiredService<ChatListPage>();
			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}
		[RelayCommand]
		private async Task AbrirBuscarPsicologoAsync()
		{
			var page = _serviceProvider.GetRequiredService<BuscarPsicologoPage>();
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