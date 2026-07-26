using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MauiApp1.Services;
using MauiApp1.Views;

namespace MauiApp1.ViewModels
{
	public partial class PerfilViewModel : ObservableObject
	{
		private readonly SessaoUsuario _sessao;
		private readonly IServiceProvider _serviceProvider;
		private readonly ChatConnectionService _chat;

		[ObservableProperty]
		private string nome = string.Empty;

		[ObservableProperty]
		private string email = string.Empty;

		[ObservableProperty]
		private string? fotoUrl;

		public string FotoExibida => string.IsNullOrEmpty(FotoUrl)
			? "avatar_placeholder.jpg"
			: $"{ApiConfig.ServidorBaseUrl}{FotoUrl}";

		partial void OnFotoUrlChanged(string? value) => OnPropertyChanged(nameof(FotoExibida));

		public PerfilViewModel(SessaoUsuario sessao, IServiceProvider serviceProvider, ChatConnectionService chat)
		{
			_sessao = sessao;
			_serviceProvider = serviceProvider;
			_chat = chat;
			AtualizarDaSessao();
		}

		public void AtualizarDaSessao()
		{
			Nome = _sessao.Nome;
			Email = _sessao.Email;
			FotoUrl = _sessao.FotoUrl;
		}

		[RelayCommand]
		private async Task IrParaHomeAsync()
		{
			await Application.Current!.MainPage!.Navigation.PopToRootAsync();
		}

		[RelayCommand]
		private async Task AbrirChatAsync()
		{
			await Application.Current!.MainPage!.Navigation.PopToRootAsync();
			var page = _serviceProvider.GetRequiredService<ChatListPage>();
			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}

		[RelayCommand]
		private async Task AbrirQuestionariosAsync()
		{
			if (_sessao.Tipo == TipoUsuarioLogado.Psicologo)
			{
				var page = _serviceProvider.GetRequiredService<QuestionariosPage>();
				await Application.Current!.MainPage!.Navigation.PushAsync(page);
			}
			else
			{
				var page = _serviceProvider.GetRequiredService<QuestionariosPacientePage>();
				await Application.Current!.MainPage!.Navigation.PushAsync(page);
			}
		}

		[RelayCommand]
		private async Task AbrirEditarPerfilAsync()
		{
			var page = _serviceProvider.GetRequiredService<EditarPerfilPage>();
			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}

		[RelayCommand]
		private async Task AbrirSuporteAsync()
			=> await Application.Current!.MainPage!.DisplayAlert("Suporte", "Canal de suporte ainda não implementado.", "OK");

		[RelayCommand]
		private async Task SairAsync()
		{
			var confirmou = await Application.Current!.MainPage!.DisplayAlert(
				"Deseja encerrar sua experiência?",
				"Você precisará entrar de novo com seu e-mail e senha na próxima vez.",
				"Sair", "Cancelar");

			if (!confirmou) return;

			await _chat.DesconectarAsync();
			_sessao.EncerrarSessao();

			var loginPage = _serviceProvider.GetRequiredService<LoginPage>();
			Application.Current!.MainPage = new NavigationPage(loginPage);
		}
	}
}
