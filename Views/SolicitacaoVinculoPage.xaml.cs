using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class SolicitacaoVinculoPage : ContentPage
{
	public SolicitacaoVinculoPage(SolicitacaoVinculoViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}