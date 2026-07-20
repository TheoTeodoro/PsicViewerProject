using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class HomePsicologoPage : ContentPage
{
	private readonly HomePsicologoViewModel _viewModel;

	public HomePsicologoPage(HomePsicologoViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_viewModel.AtualizarFoto();
	}
}
