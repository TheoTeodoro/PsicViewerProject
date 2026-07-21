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

		[ObservableProperty]
		private string nomeUsuario = string.Empty;
		// NOTA: os 3 números abaixo são MOCK (fixos), só para visualizar o
		// layout. Viram dados reais quando existirem:
		// - PacientesAtivos  -> IPacienteRepository filtrando por PsicologoId
		// - QuestionariosAtivos / RespostasPendentes -> IQuestionarioRepository,
		//   que ainda não existe (depende da entidade Questionario no Core)
		[ObservableProperty]
		private int pacientesAtivos = 15;

		[ObservableProperty]
		private int questionariosAtivos = 4;

		[ObservableProperty]
		private int respostasPendentes = 7;

		public string FotoExibida => string.IsNullOrEmpty(_sessao.FotoUrl)
			? "avatar_placeholder.jpg"
			: $"{ApiConfig.ServidorBaseUrl}{_sessao.FotoUrl}";

		public HomePsicologoViewModel(SessaoUsuario sessao, IServiceProvider serviceProvider, ChatConnectionService chat)
		{
			_sessao = sessao;
			_serviceProvider = serviceProvider;
			_chat = chat;
			NomeUsuario = string.IsNullOrWhiteSpace(_sessao.Nome)
				? "Psicólogo(a)"
				: _sessao.Nome.Split(' ')[0]; // só o primeiro nome, como no Figma ("Olá, Theo")
		}

		/// <summary>Chamado no OnAppearing da Home — pega a foto atualizada
		/// caso a pessoa tenha acabado de trocar no Perfil.</summary>
		public void AtualizarFoto() => OnPropertyChanged(nameof(FotoExibida));

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
			=> await Application.Current!.MainPage!.DisplayAlert("Em breve", "Tela de Questionários ainda não implementada.", "OK");

		[RelayCommand]
		private async Task AbrirRelatoriosAsync()
			=> await Application.Current!.MainPage!.DisplayAlert("Em breve", "Tela de Relatórios ainda não implementada.", "OK");

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