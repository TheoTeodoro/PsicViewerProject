using MauiApp1.ViewModels;
namespace MauiApp1.Views;
public partial class DadosPacientePage : ContentPage
{
	public DadosPacientePage(DadosPacienteViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}