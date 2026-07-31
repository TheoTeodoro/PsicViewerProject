using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class ChatConversaPage : ContentPage
{
	private readonly ChatConversaViewModel _viewModel;

	public ChatConversaPage(ChatConversaViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = viewModel;
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();

	
		if (!Navigation.NavigationStack.Contains(this))
		{
			_viewModel.Dispose();
		}
	}
}