using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MauiApp1.Services;
using MauiApp1.Views;
namespace MauiApp1.ViewModels
{
	public partial class CadastroPsicologoViewModel : ObservableObject
	{
		private readonly ContaApiService _conta;
		private readonly IServiceProvider _serviceProvider;
		[ObservableProperty]
		private string nome = string.Empty;
		[ObservableProperty]
		private string email = string.Empty;
		[ObservableProperty]
		private string crp = string.Empty;
		[ObservableProperty]
		private string telefone = string.Empty;
		[ObservableProperty]
		private DateTime dataNascimento = DateTime.Today.AddYears(-18);
		[ObservableProperty]
		private string generoSelecionado = string.Empty;
		[ObservableProperty]
		private string senha = string.Empty;
		[ObservableProperty]
		private string confirmarSenha = string.Empty;
		[ObservableProperty]
		private string mensagemErro = string.Empty;
		[ObservableProperty]
		private bool carregando;
		public string[] OpcoesGenero => GeneroHelper.Opcoes;

		[ObservableProperty]
		private bool mostrarSenha;

		[ObservableProperty]
		private bool mostrarConfirmarSenha;

		public bool SenhaOculta => !MostrarSenha;
		public bool ConfirmarSenhaOculta => !MostrarConfirmarSenha;
		public string IconeSenha => MostrarSenha ? "icone_olho_aberto.svg" : "icone_olho_fechado.svg";
		public string IconeConfirmarSenha => MostrarConfirmarSenha ? "icone_olho_aberto.svg" : "icone_olho_fechado.svg";

		partial void OnMostrarSenhaChanged(bool value)
		{
			OnPropertyChanged(nameof(SenhaOculta));
			OnPropertyChanged(nameof(IconeSenha));
		}

		partial void OnMostrarConfirmarSenhaChanged(bool value)
		{
			OnPropertyChanged(nameof(ConfirmarSenhaOculta));
			OnPropertyChanged(nameof(IconeConfirmarSenha));
		}

		[RelayCommand]
		private void AlternarMostrarSenha() => MostrarSenha = !MostrarSenha;

		[RelayCommand]
		private void AlternarMostrarConfirmarSenha() => MostrarConfirmarSenha = !MostrarConfirmarSenha;

		public CadastroPsicologoViewModel(ContaApiService conta, IServiceProvider serviceProvider)
		{
			_conta = conta;
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
				var (sucesso, _, erro) = await _conta.CadastrarPsicologoAsync(
					Nome, Email, Senha, Crp, Telefone, DataNascimento, GeneroHelper.ParaValorApi(GeneroSelecionado));
				if (!sucesso)
				{
					MensagemErro = erro ?? "Não foi possível criar a conta.";
					return;
				}
				var contaCriadaPage = _serviceProvider.GetRequiredService<ContaCriadaPage>();
				await Application.Current!.MainPage!.Navigation.PushAsync(contaCriadaPage);
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
	}
}