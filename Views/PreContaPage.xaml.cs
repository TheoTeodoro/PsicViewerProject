using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class PreContaPage : ContentPage
{
	public PreContaPage(PreContaViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}