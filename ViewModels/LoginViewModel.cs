using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MauiApp1.Services;
using MauiApp1.Views;
namespace MauiApp1.ViewModels
{
	public partial class LoginViewModel : ObservableObject
	{
		private readonly ContaApiService _conta;
		private readonly SessaoUsuario _sessao;
		private readonly IServiceProvider _serviceProvider;
		[ObservableProperty]
		private string email = string.Empty;
		[ObservableProperty]
		private string senha = string.Empty;
		[ObservableProperty]
		private string mensagemErro = string.Empty;
		[ObservableProperty]
		private bool carregando;

		[ObservableProperty]
		private bool mostrarSenha;

		public bool SenhaOculta => !MostrarSenha;
		public string IconeSenha => MostrarSenha ? "icone_olho_aberto.svg" : "icone_olho_fechado.svg";

		partial void OnMostrarSenhaChanged(bool value)
		{
			OnPropertyChanged(nameof(SenhaOculta));
			OnPropertyChanged(nameof(IconeSenha));
		}

		[RelayCommand]
		private void AlternarMostrarSenha() => MostrarSenha = !MostrarSenha;

		public LoginViewModel(ContaApiService conta, SessaoUsuario sessao, IServiceProvider serviceProvider)
		{
			_conta = conta;
			_sessao = sessao;
			_serviceProvider = serviceProvider;
		}
		[RelayCommand]
		private async Task EntrarAsync()
		{
			MensagemErro = string.Empty;
			if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Senha))
			{
				MensagemErro = "Preencha e-mail e senha.";
				return;
			}
			Carregando = true;
			try
			{
				var (sucesso, usuario, erro) = await _conta.LoginAsync(Email, Senha);
				if (!sucesso || usuario is null)
				{
					MensagemErro = erro ?? "E-mail ou senha inválidos.";
					return;
				}
				if (usuario.Tipo == "Paciente")
				{
					_sessao.IniciarComoPaciente(usuario.Id, usuario.Nome, usuario.Email, usuario.FotoUrl);
					var home = _serviceProvider.GetRequiredService<HomePacientePage>();
					Application.Current!.MainPage = new NavigationPage(home);
				}
				else
				{
					_sessao.IniciarComoPsicologo(usuario.Id, usuario.Nome, usuario.Email, usuario.FotoUrl);
					var home = _serviceProvider.GetRequiredService<HomePsicologoPage>();
					Application.Current!.MainPage = new NavigationPage(home);
				}
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível conectar ao servidor: " + ex.Message;
			}
			finally
			{
				Carregando = false;
			}
		}
		[RelayCommand]
		private async Task IrParaCadastroAsync()
		{
			var preContaPage = _serviceProvider.GetRequiredService<PreContaPage>();
			await Application.Current!.MainPage!.Navigation.PushAsync(preContaPage);
		}
	}
}