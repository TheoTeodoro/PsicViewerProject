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

		[ObservableProperty]
		private string nomeUsuario = string.Empty;

		// NOTA: MOCK — vira real quando existir RegistrarHumorUseCase (Application)
		// e a leitura do último registro via IRegistroHumorRepository.
		[ObservableProperty]
		private bool jaRegistrouHumorHoje = false;

		public string FotoExibida => string.IsNullOrEmpty(_sessao.FotoUrl)
			? "avatar_placeholder.jpg"
			: $"{ApiConfig.ServidorBaseUrl}{_sessao.FotoUrl}";

		public HomePacienteViewModel(SessaoUsuario sessao, IServiceProvider serviceProvider, ChatConnectionService chat)
		{
			_sessao = sessao;
			_serviceProvider = serviceProvider;
			_chat = chat;
			NomeUsuario = string.IsNullOrWhiteSpace(_sessao.Nome)
				? "Paciente"
				: _sessao.Nome.Split(' ')[0];
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
		private async Task RegistrarHumorAsync()
			=> await Application.Current!.MainPage!.DisplayAlert("Em breve", "Registro de humor ainda não implementado.", "OK");

		[RelayCommand]
		private async Task AbrirHistoricoAsync()
			=> await Application.Current!.MainPage!.DisplayAlert("Em breve", "Histórico ainda não implementado.", "OK");

		[RelayCommand]
		private async Task AbrirQuestionariosAsync()
			=> await Application.Current!.MainPage!.DisplayAlert("Em breve", "Questionários ainda não implementados.", "OK");

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