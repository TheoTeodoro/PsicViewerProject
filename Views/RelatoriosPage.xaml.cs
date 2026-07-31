using MauiApp1.ViewModels;
namespace MauiApp1.Views;
public partial class RelatoriosPage : ContentPage
{
	private readonly RelatoriosViewModel _viewModel;
	public RelatoriosPage(RelatoriosViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = viewModel;

		
		_viewModel.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == nameof(RelatoriosViewModel.TemDados) || e.PropertyName == nameof(RelatoriosViewModel.Grafico))
				GraphicsViewGrafico.Invalidate();
		};
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_viewModel.CarregarCommand.Execute(null);
	}

	
	private void OnGraficoTocado(object? sender, TouchEventArgs e)
	{
		if (e.Touches.Length == 0) return;
		var toque = e.Touches[0];
		_viewModel.AoTocarNoGrafico(toque.X, toque.Y);
	}
}