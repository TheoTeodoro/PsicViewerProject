using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Services;

namespace MauiApp1.ViewModels
{
	public partial class SolicitacaoVinculoViewModel : ObservableObject
	{
		private readonly VinculoApiService _vinculo;
		private VinculoDto? _convite;

		[ObservableProperty]
		private string nomeSolicitante = string.Empty;

		[ObservableProperty]
		private string crpSolicitante = string.Empty;

		[ObservableProperty]
		private string fotoExibida = "avatar_placeholder.jpg";

		[ObservableProperty]
		private bool carregando;

		[ObservableProperty]
		private string mensagemErro = string.Empty;

		public SolicitacaoVinculoViewModel(VinculoApiService vinculo)
		{
			_vinculo = vinculo;
		}

		public void Definir(VinculoDto convite)
		{
			_convite = convite;
			NomeSolicitante = convite.ContatoNome;
			CrpSolicitante = string.IsNullOrEmpty(convite.ContatoCrp) ? string.Empty : $"CRP {convite.ContatoCrp}";
			FotoExibida = string.IsNullOrEmpty(convite.ContatoFotoUrl)
				? "avatar_placeholder.jpg"
				: $"{ApiConfig.ServidorBaseUrl}{convite.ContatoFotoUrl}";
		}

		[RelayCommand]
		private async Task AceitarAsync()
		{
			if (_convite is null) return;

			Carregando = true;
			try
			{
				var ok = await _vinculo.AceitarAsync(_convite.Id);
				if (ok)
					await Application.Current!.MainPage!.Navigation.PopAsync();
				else
					MensagemErro = "Não foi possível aceitar o vínculo.";
			}
			finally
			{
				Carregando = false;
			}
		}

		[RelayCommand]
		private async Task RecusarAsync()
		{
			if (_convite is null) return;

			Carregando = true;
			try
			{
				var ok = await _vinculo.RecusarAsync(_convite.Id);
				if (ok)
					await Application.Current!.MainPage!.Navigation.PopAsync();
				else
					MensagemErro = "Não foi possível recusar o vínculo.";
			}
			finally
			{
				Carregando = false;
			}
		}
	}
}