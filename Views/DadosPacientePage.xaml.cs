using System.Threading;
using System.Threading.Tasks;
using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class DadosPacientePage : ContentPage
{
	private readonly DadosPacienteViewModel _viewModel;

	// Controla se o botão está "armado" esperando o segundo toque (Desvincular)
	private bool _confirmandoDesvinculo = false;

	// Cancela o timer de 3 segundos se o usuário tocar de novo a tempo
	private CancellationTokenSource _resetCts;

	public DadosPacientePage(DadosPacienteViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = viewModel;
	}

	private async void OnBotaoVinculoClicked(object sender, EventArgs e)
	{
		if (!_confirmandoDesvinculo)
		{
			// Primeiro toque: arma o botão (fica vermelho e escrito "Desvincular" por 3s)
			_confirmandoDesvinculo = true;
			BotaoVinculo.Text = "Desvincular";
			BotaoVinculo.BackgroundColor = Color.FromArgb("#E74C3C");

			_resetCts?.Cancel();
			_resetCts = new CancellationTokenSource();
			var token = _resetCts.Token;

			try
			{
				await Task.Delay(3000, token);
				if (!token.IsCancellationRequested)
				{
					ResetarBotaoVinculo();
				}
			}
			catch (TaskCanceledException)
			{
				// Cancelado porque o usuário tocou de novo dentro da janela de 3s — segue o fluxo normal abaixo
			}

			return;
		}

		// Segundo toque, dentro da janela de 3 segundos: cancela o timer e pede confirmação
		_resetCts?.Cancel();
		ResetarBotaoVinculo();

		bool confirmar = await DisplayAlert(
			"Desvincular paciente",
			$"Tem certeza que deseja desvincular {_viewModel.Nome}? O histórico compartilhado, o contato no chat e a opção de atribuir questionários a este paciente serão removidos.",
			"Sim, desvincular",
			"Cancelar");

		if (!confirmar)
			return;

		BotaoVinculo.IsEnabled = false;

		try
		{
			var sucesso = await _viewModel.DesvincularPacienteAsync();
			if (sucesso)
			{
				await Navigation.PopAsync();
			}
			else
			{
				await DisplayAlert("Erro", _viewModel.MensagemErro, "OK");
			}
		}
		finally
		{
			BotaoVinculo.IsEnabled = true;
		}
	}

	private void ResetarBotaoVinculo()
	{
		_confirmandoDesvinculo = false;
		BotaoVinculo.Text = "Vínculo Ativo";
		BotaoVinculo.BackgroundColor = (Color)Application.Current.Resources["AzulPrimario"];
	}
}