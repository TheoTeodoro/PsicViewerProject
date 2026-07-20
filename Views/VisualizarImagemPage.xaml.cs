namespace MauiApp1.Views;

public partial class VisualizarImagemPage : ContentPage
{
	public VisualizarImagemPage(string urlImagem)
	{
		InitializeComponent();
		ImagemExibida.Source = urlImagem;
	}

	private async void OnFecharClicked(object sender, EventArgs e)
	{
		await Navigation.PopModalAsync();
	}

	private async void OnFundoTocado(object sender, TappedEventArgs e)
	{
		await Navigation.PopModalAsync();
	}
}
