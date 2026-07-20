using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PsicViewer.Core.Entities;
using PsicViewer.Core.Interfaces;
using MauiApp1.Views;

namespace MauiApp1.ViewModels
{
	public partial class CadastroPsicologoViewModel : ObservableObject
	{
		private readonly IPsicologoRepository _psicologos;
		private readonly IServiceProvider _serviceProvider;

		[ObservableProperty]
		private string nome = string.Empty;

		[ObservableProperty]
		private string email = string.Empty;

		[ObservableProperty]
		private string crp = string.Empty;

		[ObservableProperty]
		private string senha = string.Empty;

		[ObservableProperty]
		private string confirmarSenha = string.Empty;

		[ObservableProperty]
		private string mensagemErro = string.Empty;

		[ObservableProperty]
		private bool carregando;

		public CadastroPsicologoViewModel(IPsicologoRepository psicologos, IServiceProvider serviceProvider)
		{
			_psicologos = psicologos;
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
				var existentePorEmail = await _psicologos.ObterPorEmailAsync(Email);
				if (existentePorEmail is not null)
				{
					MensagemErro = "Já existe uma conta com esse e-mail.";
					return;
				}

				var existentePorCrp = await _psicologos.ObterPorCrpAsync(Crp);
				if (existentePorCrp is not null)
				{
					MensagemErro = "Já existe uma conta cadastrada com esse CRP.";
					return;
				}

				var psicologo = new Psicologo(Nome, Email, Senha, Crp);
				await _psicologos.SalvarAsync(psicologo);

				var contaCriadaPage = _serviceProvider.GetRequiredService<ContaCriadaPage>();
				await Application.Current!.MainPage!.Navigation.PushAsync(contaCriadaPage);
			}
			catch (ArgumentException ex)
			{
				MensagemErro = ex.Message;
			}
			catch (Exception ex)
			{
				MensagemErro = "Erro inesperado: " + ex.Message;
			}
			finally
			{
				Carregando = false;
			}
		}
	}
}