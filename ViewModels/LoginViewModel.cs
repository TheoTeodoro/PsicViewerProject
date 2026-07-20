using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PsicViewer.Core.Interfaces;
using MauiApp1.Services;
using MauiApp1.Views;

namespace MauiApp1.ViewModels
{
	public partial class LoginViewModel : ObservableObject
	{
		private readonly IPacienteRepository _pacientes;
		private readonly IPsicologoRepository _psicologos;
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

		public LoginViewModel(
			IPacienteRepository pacientes,
			IPsicologoRepository psicologos,
			SessaoUsuario sessao,
			IServiceProvider serviceProvider)
		{
			_pacientes = pacientes;
			_psicologos = psicologos;
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
				// NOTA: comparação de senha em texto puro é só para teste
				// nessa fase sem backend. Trocar por hash (BCrypt) + IAuthService.
				var paciente = await _pacientes.ObterPorEmailAsync(Email);
				if (paciente is not null && paciente.SenhaHash == Senha)
				{
					_sessao.IniciarComoPaciente(paciente.Id, paciente.Nome, paciente.Email, paciente.FotoUrl);
					var home = _serviceProvider.GetRequiredService<HomePacientePage>();
					Application.Current!.MainPage = new NavigationPage(home);
					return;
				}

				var psicologo = await _psicologos.ObterPorEmailAsync(Email);
				if (psicologo is not null && psicologo.SenhaHash == Senha)
				{
					_sessao.IniciarComoPsicologo(psicologo.Id, psicologo.Nome, psicologo.Email, psicologo.FotoUrl);
					var home = _serviceProvider.GetRequiredService<HomePsicologoPage>();
					Application.Current!.MainPage = new NavigationPage(home);
					return;
				}

				MensagemErro = "E-mail ou senha inválidos.";
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
