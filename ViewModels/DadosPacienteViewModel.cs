using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Services;

namespace MauiApp1.ViewModels
{
	public partial class DadosPacienteViewModel : ObservableObject
	{
		private readonly PacientePerfilPublicoService _perfilPublico;
		private readonly VinculoApiService _vinculo;

		// Guardado aqui em CarregarAsync — é o que EncerrarAsync usa pra
		// saber QUAL vínculo desfazer (o PacienteId sozinho não basta).
		private Guid _vinculoId;

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

		[ObservableProperty]
		private string emailExibido = string.Empty;

		[ObservableProperty]
		private string telefoneExibido = string.Empty;
		partial void OnFotoUrlChanged(string? value) => OnPropertyChanged(nameof(FotoExibida));

		public DadosPacienteViewModel(PacientePerfilPublicoService perfilPublico, VinculoApiService vinculo)
		{
			_perfilPublico = perfilPublico;
			_vinculo = vinculo;
		}

		public async Task CarregarAsync(Guid pacienteId, Guid vinculoId)
		{
			_vinculoId = vinculoId;
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
				EmailExibido = string.IsNullOrWhiteSpace(dados.Email)
				? "Não informado"
				: dados.Email;
				TelefoneExibido = string.IsNullOrWhiteSpace(dados.Telefone)
				? "Não informado"
					: dados.Telefone;
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

		/// <summary>Encerra o vínculo com este paciente. Retorna true se deu
		/// certo — quem chama (a Page) decide se volta ou não com base nisso.</summary>
		public async Task<bool> DesvincularPacienteAsync()
		{
			MensagemErro = string.Empty;
			try
			{
				var ok = await _vinculo.EncerrarAsync(_vinculoId);
				if (!ok)
					MensagemErro = "Não foi possível desvincular o paciente.";
				return ok;
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível conectar ao servidor: " + ex.Message;
				return false;
			}
		}

		[RelayCommand]
		private async Task VoltarAsync() => await Application.Current!.MainPage!.Navigation.PopAsync();
	}
}