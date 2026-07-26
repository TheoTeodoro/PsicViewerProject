using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class HomePacientePage : ContentPage
{
	private readonly HomePacienteViewModel _viewModel;

	public HomePacientePage(HomePacienteViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_viewModel.AtualizarFoto();
		_ = _viewModel.VerificarNotificacoesAsync();
	}
}