using System.Collections.Generic;
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
		private readonly QuestionarioApiService _questionarios;

		private Guid _proximoQuestionarioId;
		private Guid _proximaPerguntaId;

		[ObservableProperty]
		private string nomeUsuario = string.Empty;
		[ObservableProperty]
		private bool temNotificacao;
		[ObservableProperty]
		private int notificacoesNaoLidas;
		[ObservableProperty]
		private bool temNotificacaoChat;

		public string FotoExibida => string.IsNullOrEmpty(_sessao.FotoUrl)
			? "avatar_placeholder.jpg"
			: $"{ApiConfig.ServidorBaseUrl}{_sessao.FotoUrl}";


		[ObservableProperty]
		private bool temProximaPergunta;

		[ObservableProperty]
		private string proximoQuestionarioTitulo = string.Empty;

		[ObservableProperty]
		private string proximaPerguntaTexto = string.Empty;

		[ObservableProperty]
		private string proximoHorario = string.Empty;

		[ObservableProperty]
		private string proximoTipo = string.Empty; // "Escala" | "Texto" | "MultiplaEscolha"

		[ObservableProperty]
		private List<string> proximasOpcoes = new();

		[ObservableProperty]
		private int? proximaEscalaSelecionada;

		[ObservableProperty]
		private string? proximaOpcaoSelecionada;

		[ObservableProperty]
		private string proximaRespostaTexto = string.Empty;

		[ObservableProperty]
		private bool enviandoProximaPergunta;

		[ObservableProperty]
		private string proximaMensagemErro = string.Empty;

		public bool ProximaEhEscala => ProximoTipo == "Escala";
		public bool ProximaEhTexto => ProximoTipo == "Texto";
		public bool ProximaEhMultiplaEscolha => ProximoTipo == "MultiplaEscolha";

		partial void OnProximoTipoChanged(string value)
		{
			OnPropertyChanged(nameof(ProximaEhEscala));
			OnPropertyChanged(nameof(ProximaEhTexto));
			OnPropertyChanged(nameof(ProximaEhMultiplaEscolha));
		}

		public double Escala1Tamanho => ProximaEscalaSelecionada == 1 ? 1.15 : 1.0;
		public double Escala2Tamanho => ProximaEscalaSelecionada == 2 ? 1.15 : 1.0;
		public double Escala3Tamanho => ProximaEscalaSelecionada == 3 ? 1.15 : 1.0;
		public double Escala4Tamanho => ProximaEscalaSelecionada == 4 ? 1.15 : 1.0;
		public double Escala5Tamanho => ProximaEscalaSelecionada == 5 ? 1.15 : 1.0;

		partial void OnProximaEscalaSelecionadaChanged(int? value)
		{
			OnPropertyChanged(nameof(Escala1Tamanho));
			OnPropertyChanged(nameof(Escala2Tamanho));
			OnPropertyChanged(nameof(Escala3Tamanho));
			OnPropertyChanged(nameof(Escala4Tamanho));
			OnPropertyChanged(nameof(Escala5Tamanho));
		}

		public HomePacienteViewModel(SessaoUsuario sessao, IServiceProvider serviceProvider, ChatConnectionService chat,
			VinculoApiService vinculo, NotificacaoService notificacoes, QuestionarioApiService questionarios)
		{
			_sessao = sessao;
			_serviceProvider = serviceProvider;
			_chat = chat;
			_vinculo = vinculo;
			_notificacoes = notificacoes;
			_questionarios = questionarios;
			NomeUsuario = string.IsNullOrWhiteSpace(_sessao.Nome)
				? "Paciente"
				: _sessao.Nome.Split(' ')[0];

			_chat.MensagemRecebida += (s, e) => MainThread.BeginInvokeOnMainThread(() => _ = VerificarNotificacoesAsync());
		}

		public void AtualizarFoto() => OnPropertyChanged(nameof(FotoExibida));

		public async Task VerificarNotificacoesAsync()
		{
			try
			{
				if (!_chat.Conectado)
					await _chat.ConectarAsync(_sessao.UsuarioId);

				var todas = await _notificacoes.ObterNotificacoesAsync();
				NotificacoesNaoLidas = todas.Count(i => i.NaoLida);
				TemNotificacao = todas.Any(i => i.Tipo == TipoNotificacao.SolicitacaoVinculo && i.NaoLida);
				TemNotificacaoChat = todas.Any(i => i.NaoLida && i.EhMensagemChat);

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

			}
		}

		public async Task CarregarProximaPerguntaAsync()
		{
			try
			{
				var proxima = await _questionarios.ObterProximaPerguntaAsync(_sessao.UsuarioId);

				TemProximaPergunta = proxima?.TemPergunta ?? false;
				if (!TemProximaPergunta) return;

				_proximoQuestionarioId = proxima!.QuestionarioId;
				_proximaPerguntaId = proxima.PerguntaId;
				ProximoQuestionarioTitulo = proxima.QuestionarioTitulo;
				ProximaPerguntaTexto = proxima.PerguntaTexto;
				ProximoHorario = proxima.Horario;
				ProximoTipo = proxima.Tipo;

				ProximasOpcoes = string.IsNullOrEmpty(proxima.Opcoes)
					? new List<string>()
					: proxima.Opcoes.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();

				ProximaEscalaSelecionada = null;
				ProximaOpcaoSelecionada = null;
				ProximaRespostaTexto = string.Empty;
				ProximaMensagemErro = string.Empty;
			}
			catch
			{
				TemProximaPergunta = false;
			}
		}

		[RelayCommand]
		private void SelecionarProximaEscala(string valor)
		{
			if (int.TryParse(valor, out var v)) ProximaEscalaSelecionada = v;
		}

		[RelayCommand]
		private void SelecionarProximaOpcao(string opcao) => ProximaOpcaoSelecionada = opcao;

		[RelayCommand]
		private async Task EnviarProximaPerguntaAsync()
		{
			if (!TemProximaPergunta) return;

			string? valorPrincipal = ProximaEhMultiplaEscolha ? ProximaOpcaoSelecionada
				: ProximaEhTexto ? ProximaRespostaTexto.Trim()
				: null;
			int? valorEscala = ProximaEhEscala ? ProximaEscalaSelecionada : null;

			if (ProximaEhEscala && valorEscala is null)
			{
				ProximaMensagemErro = "Escolha uma opção antes de enviar.";
				return;
			}
			if (ProximaEhMultiplaEscolha && string.IsNullOrEmpty(valorPrincipal))
			{
				ProximaMensagemErro = "Escolha uma opção antes de enviar.";
				return;
			}
			if (ProximaEhTexto && string.IsNullOrWhiteSpace(valorPrincipal))
			{
				ProximaMensagemErro = "Escreva algo antes de enviar.";
				return;
			}

			ProximaMensagemErro = string.Empty;
			EnviandoProximaPergunta = true;
			try
			{
				var (sucesso, erro) = await _questionarios.ResponderPerguntaAsync(
					_proximoQuestionarioId, _proximaPerguntaId, _sessao.UsuarioId, valorEscala, valorPrincipal, null, null);

				if (!sucesso)
				{
					ProximaMensagemErro = erro ?? "Não foi possível enviar essa resposta.";
					return;
				}

				var page = _serviceProvider.GetRequiredService<ResponderQuestionarioPage>();
				if (page.BindingContext is ResponderQuestionarioViewModel vm)
					await vm.CarregarAsync(_proximoQuestionarioId);

				await Application.Current!.MainPage!.Navigation.PushAsync(page);

				TemProximaPergunta = false;
			}
			catch (Exception ex)
			{
				ProximaMensagemErro = "Não foi possível conectar ao servidor: " + ex.Message;
			}
			finally
			{
				EnviandoProximaPergunta = false;
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