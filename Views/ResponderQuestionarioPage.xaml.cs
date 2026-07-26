using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class ResponderQuestionarioPage : ContentPage
{
	public ResponderQuestionarioPage(ResponderQuestionarioViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
