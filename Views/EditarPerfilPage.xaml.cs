using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class EditarPerfilPage : ContentPage
{
	private readonly EditarPerfilViewModel _viewModel;

	public EditarPerfilPage(EditarPerfilViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_ = _viewModel.CarregarAsync();
	}

	private async void OnVoltarClicked(object sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}
}