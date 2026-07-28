using MauiApp1.ViewModels;
namespace MauiApp1.Views;
public partial class HistoricoPacientePage : ContentPage
{
	public HistoricoPacientePage(HistoricoPacienteViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
