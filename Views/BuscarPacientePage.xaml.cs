using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class BuscarPacientePage : ContentPage
{
	private readonly BuscarPacienteViewModel _viewModel;

	public BuscarPacientePage(BuscarPacienteViewModel viewModel)
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