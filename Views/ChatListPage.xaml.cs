using MauiApp1.ViewModels;
namespace MauiApp1.Views;
public partial class ChatListPage : ContentPage
{
	private readonly ChatListViewModel _viewModel;
	public ChatListPage(ChatListViewModel viewModel)
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