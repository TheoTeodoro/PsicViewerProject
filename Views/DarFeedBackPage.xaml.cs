using MauiApp1.ViewModels;
namespace MauiApp1.Views;
public partial class DarFeedbackPage : ContentPage
{
	public DarFeedbackPage(DarFeedbackViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}