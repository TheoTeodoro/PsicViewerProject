using MauiApp1.ViewModels;
namespace MauiApp1.Views;
public partial class QuestionariosPage : ContentPage
{
	private readonly QuestionariosViewModel _viewModel;
	public QuestionariosPage(QuestionariosViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = viewModel;
	}
	protected override void OnAppearing()
	{
		base.OnAppearing();
		_viewModel.CarregarCommand.Execute(null);
	}
}
