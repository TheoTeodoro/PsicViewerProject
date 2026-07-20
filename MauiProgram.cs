using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using PsicViewer.Core.Interfaces;
using PsicViewer.Infrastructure.Repositories;
using MauiApp1.Services;
using MauiApp1.ViewModels;
using MauiApp1.Views;

namespace MauiApp1
{
	public static class MauiProgram
	{
		public static MauiApp CreateMauiApp()
		{
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
					fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				});

			// Repositórios (em memória por enquanto)
			builder.Services.AddSingleton<IPacienteRepository, PacienteRepositoryInMemory>();
			builder.Services.AddSingleton<IPsicologoRepository, PsicologoRepositoryInMemory>();

			// Sessão do usuário logado
			builder.Services.AddSingleton<SessaoUsuario>();

			// Chat: conexão SignalR + upload de arquivo + gravador de áudio
			builder.Services.AddSingleton<ChatConnectionService>();
			builder.Services.AddSingleton<ArquivoUploadService>();
			builder.Services.AddSingleton(AudioManager.Current);

			// Páginas
			builder.Services.AddTransient<LoginPage>();
			builder.Services.AddTransient<PreContaPage>();
			builder.Services.AddTransient<CadastroPacientePage>();
			builder.Services.AddTransient<CadastroPsicologoPage>();
			builder.Services.AddTransient<ContaCriadaPage>();
			builder.Services.AddTransient<HomePacientePage>();
			builder.Services.AddTransient<HomePsicologoPage>();
			builder.Services.AddTransient<ChatListPage>();
			builder.Services.AddTransient<ChatConversaPage>();
			builder.Services.AddTransient<PerfilPage>();
			builder.Services.AddTransient<EditarPerfilPage>();

			// ViewModels
			builder.Services.AddTransient<LoginViewModel>();
			builder.Services.AddTransient<PreContaViewModel>();
			builder.Services.AddTransient<CadastroPacienteViewModel>();
			builder.Services.AddTransient<CadastroPsicologoViewModel>();
			builder.Services.AddTransient<HomePacienteViewModel>();
			builder.Services.AddTransient<HomePsicologoViewModel>();
			builder.Services.AddTransient<ChatListViewModel>();
			builder.Services.AddTransient<ChatConversaViewModel>();
			builder.Services.AddTransient<PerfilViewModel>();
			builder.Services.AddTransient<EditarPerfilViewModel>();

#if DEBUG
			builder.Logging.AddDebug();
#endif
			return builder.Build();
		}
	}
}