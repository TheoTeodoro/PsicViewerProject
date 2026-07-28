using MauiApp1.ViewModels;
namespace MauiApp1.Views;
public partial class DetalheHistoricoQuestionarioPage : ContentPage
{
	public DetalheHistoricoQuestionarioPage(DetalheHistoricoQuestionarioViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
