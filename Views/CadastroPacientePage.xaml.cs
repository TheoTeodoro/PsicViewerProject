using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class CadastroPacientePage : ContentPage
{
	public CadastroPacientePage(CadastroPacienteViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
