using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class BuscarPsicologoPage : ContentPage
{
	private readonly BuscarPsicologoViewModel _viewModel;

	public BuscarPsicologoPage(BuscarPsicologoViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_viewModel.CarregarCommand.Execute(null);
	}
}