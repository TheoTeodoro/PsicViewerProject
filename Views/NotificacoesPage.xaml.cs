using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class NotificacoesPage : ContentPage
{
	private readonly NotificacoesViewModel _viewModel;

	public NotificacoesPage(NotificacoesViewModel viewModel)
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