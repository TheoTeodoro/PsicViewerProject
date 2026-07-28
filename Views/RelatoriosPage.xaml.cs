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

		// GraphicsView não redesenha sozinho quando os dados internos do
		// Drawable mudam — só quando o layout muda de tamanho. Antes só
		// escutava TemDados, mas TemDados só muda de valor quando passa
		// de "tinha dado" pra "não tinha" (ou vice-versa) — gerar um
		// segundo gráfico que TAMBÉM tem dado não disparava redesenho
		// nenhum, e a tela ficava presa mostrando o gráfico anterior até
		// desmarcar tudo (TemDados vira false) e marcar de novo. Agora
		// também escuta Grafico, que é notificado em TODA chamada de
		// CarregarGraficoAsync, com ou sem mudança de TemDados.
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

	/// <summary>Toque no gráfico — repassa a posição (já nas coordenadas
	/// do próprio GraphicsView, iguais às usadas pra desenhar) pro
	/// ViewModel decidir se acertou algum ponto.</summary>
	private void OnGraficoTocado(object? sender, TouchEventArgs e)
	{
		if (e.Touches.Length == 0) return;
		var toque = e.Touches[0];
		_viewModel.AoTocarNoGrafico(toque.X, toque.Y);
	}
}