using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class CadastroPsicologoPage : ContentPage
{
	public CadastroPsicologoPage(CadastroPsicologoViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
