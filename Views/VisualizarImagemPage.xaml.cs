namespace MauiApp1.Views;

public partial class VisualizarImagemPage : ContentPage
{
	// Evita fechar duas vezes o mesmo modal — o toque no X e o toque de
	// fundo (que fica por baixo do botão) podiam disparar ao mesmo tempo,
	// e a segunda tentativa de PopModalAsync falhava porque o modal já
	// tinha sido fechado pela primeira.
	private bool _fechando;

	public VisualizarImagemPage(string urlImagem)
	{
		InitializeComponent();
		ImagemExibida.Source = urlImagem;
	}

	private async void OnFecharClicked(object sender, EventArgs e)
	{
		await FecharAsync();
	}

	private async void OnFundoTocado(object sender, TappedEventArgs e)
	{
		await FecharAsync();
	}

	private async Task FecharAsync()
	{
		if (_fechando) return;
		_fechando = true;

		try
		{
			if (Navigation.ModalStack.Count > 0)
				await Navigation.PopModalAsync();
		}
		catch (InvalidOperationException)
		{
			// Já foi fechado por outro caminho (ex: botão "voltar" do
			// Android) — nada a fazer, ignora com segurança.
		}
	}
}