using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class QuestionariosPacientePage : ContentPage
{
	private readonly QuestionariosPacienteViewModel _viewModel;

	public QuestionariosPacientePage(QuestionariosPacienteViewModel viewModel)
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

	private async void OnVoltarClicked(object sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}
}
