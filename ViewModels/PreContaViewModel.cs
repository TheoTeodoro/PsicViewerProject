using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MauiApp1.Views;

namespace MauiApp1.ViewModels
{
	public partial class PreContaViewModel : ObservableObject
	{
		private readonly IServiceProvider _serviceProvider;

		public PreContaViewModel(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		[RelayCommand]
		private async Task SouPacienteAsync()
		{
			var page = _serviceProvider.GetRequiredService<CadastroPacientePage>();
			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}

		[RelayCommand]
		private async Task SouPsicologoAsync()
		{
			var page = _serviceProvider.GetRequiredService<CadastroPsicologoPage>();
			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}
	}
}
