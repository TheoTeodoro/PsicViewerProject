namespace MauiApp1.Views;

public partial class ContaCriadaPage : ContentPage
{
	public ContaCriadaPage()
	{
		InitializeComponent();
	}

	private async void OnIrParaLoginClicked(object sender, EventArgs e)
	{
		// Volta pra primeira tela da pilha de navegação (o Login),
		// descartando Pré-Conta, Cadastro e essa própria tela.
		await Navigation.PopToRootAsync();
	}
}
