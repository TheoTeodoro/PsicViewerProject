using Microsoft.Extensions.DependencyInjection;
using MauiApp1.Views;
namespace MauiApp1;
public partial class App : Application
{
	public App(IServiceProvider serviceProvider)
	{
		InitializeComponent();
		UserAppTheme = AppTheme.Light;
		var loginPage = serviceProvider.GetRequiredService<LoginPage>();
		MainPage = new NavigationPage(loginPage);
	}
}