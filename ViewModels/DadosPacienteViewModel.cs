using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Services;

namespace MauiApp1.ViewModels
{
	public partial class DadosPacienteViewModel : ObservableObject
	{
		private readonly PacientePerfilPublicoService _perfilPublico;

		[ObservableProperty]
		private string nome = string.Empty;

		[ObservableProperty]
		private string? fotoUrl;

		[ObservableProperty]
		private string idadeExibida = string.Empty;

		[ObservableProperty]
		private string generoExibido = string.Empty;

		[ObservableProperty]
		private bool carregando;

		[ObservableProperty]
		private string mensagemErro = string.Empty;

		public string FotoExibida => string.IsNullOrEmpty(FotoUrl)
			? "avatar_placeholder.jpg"
			: $"{ApiConfig.ServidorBaseUrl}{FotoUrl}";

		partial void OnFotoUrlChanged(string? value) => OnPropertyChanged(nameof(FotoExibida));

		public DadosPacienteViewModel(PacientePerfilPublicoService perfilPublico)
		{
			_perfilPublico = perfilPublico;
		}

		public async Task CarregarAsync(Guid pacienteId)
		{
			Carregando = true;
			MensagemErro = string.Empty;
			try
			{
				var dados = await _perfilPublico.ObterAsync(pacienteId);
				if (dados is null)
				{
					MensagemErro = "Não foi possível carregar os dados desse paciente.";
					return;
				}

				Nome = dados.Nome;
				FotoUrl = dados.FotoUrl;
				IdadeExibida = dados.Idade is int idade ? $"{idade} anos" : "Idade não informada";
				GeneroExibido = string.IsNullOrEmpty(dados.Genero) ? "Não informado" : dados.Genero;
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
		private async Task VoltarAsync() => await Application.Current!.MainPage!.Navigation.PopAsync();
	}
}