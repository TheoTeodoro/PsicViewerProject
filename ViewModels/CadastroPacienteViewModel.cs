using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PsicViewer.Core.Entities;
using PsicViewer.Core.Interfaces;
using MauiApp1.Views;

namespace MauiApp1.ViewModels
{
	public partial class CadastroPacienteViewModel : ObservableObject
	{
		private readonly IPacienteRepository _pacientes;
		private readonly IServiceProvider _serviceProvider;

		[ObservableProperty]
		private string nome = string.Empty;

		[ObservableProperty]
		private string email = string.Empty;

		[ObservableProperty]
		private string senha = string.Empty;

		[ObservableProperty]
		private string confirmarSenha = string.Empty;

		[ObservableProperty]
		private string mensagemErro = string.Empty;

		[ObservableProperty]
		private bool carregando;

		public CadastroPacienteViewModel(IPacienteRepository pacientes, IServiceProvider serviceProvider)
		{
			_pacientes = pacientes;
			_serviceProvider = serviceProvider;
		}

		[RelayCommand]
		private async Task CriarContaAsync()
		{
			MensagemErro = string.Empty;

			if (Senha != ConfirmarSenha)
			{
				MensagemErro = "As senhas não coincidem.";
				return;
			}

			Carregando = true;
			try
			{
				var existente = await _pacientes.ObterPorEmailAsync(Email);
				if (existente is not null)
				{
					MensagemErro = "Já existe uma conta com esse e-mail.";
					return;
				}

				// NOTA: senha em texto puro apenas nessa fase sem backend/hash.
				var paciente = new Paciente(Nome, Email, Senha);
				await _pacientes.SalvarAsync(paciente);

				var contaCriadaPage = _serviceProvider.GetRequiredService<ContaCriadaPage>();
				await Application.Current!.MainPage!.Navigation.PushAsync(contaCriadaPage);
			}
			catch (ArgumentException ex)
			{
				MensagemErro = ex.Message;
			}
			finally
			{
				Carregando = false;
			}
		}
	}
}
