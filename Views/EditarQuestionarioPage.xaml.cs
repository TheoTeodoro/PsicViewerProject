using MauiApp1.ViewModels;
namespace MauiApp1.Views;
public partial class EditarQuestionarioPage : ContentPage
{
	public EditarQuestionarioPage(EditarQuestionarioViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}