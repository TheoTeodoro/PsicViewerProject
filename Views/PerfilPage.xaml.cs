using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class PerfilPage : ContentPage
{
	private readonly PerfilViewModel _viewModel;

	public PerfilPage(PerfilViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_viewModel.AtualizarDaSessao();
	}
	private async void OnVoltarClicked(object sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}
}