using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
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
			// Sessão do usuário logado
			builder.Services.AddSingleton<SessaoUsuario>();
			// Conta (cadastro/login/perfil) — fala com a API via HTTP.
			builder.Services.AddSingleton<ContaApiService>();
			builder.Services.AddSingleton<VinculoApiService>();
			builder.Services.AddSingleton<NotificacaoService>();
			builder.Services.AddSingleton<QuestionarioApiService>();
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
			builder.Services.AddTransient<BuscarPsicologoPage>();
			builder.Services.AddTransient<PacientesPage>();
			builder.Services.AddTransient<BuscarPacientePage>();
			builder.Services.AddTransient<SolicitacaoVinculoPage>();
			builder.Services.AddTransient<NotificacoesPage>();
			builder.Services.AddTransient<QuestionariosPage>();
			builder.Services.AddTransient<CriarQuestionarioPage>();
			builder.Services.AddTransient<EditarQuestionarioPage>();
			builder.Services.AddTransient<QuestionariosPacientePage>();
			builder.Services.AddTransient<ResponderQuestionarioPage>();
			builder.Services.AddTransient<DarFeedbackPage>();
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
			builder.Services.AddTransient<BuscarPsicologoViewModel>();
			builder.Services.AddTransient<PacientesViewModel>();
			builder.Services.AddTransient<BuscarPacienteViewModel>();
			builder.Services.AddTransient<SolicitacaoVinculoViewModel>();
			builder.Services.AddTransient<NotificacoesViewModel>();
			builder.Services.AddTransient<QuestionariosViewModel>();
			builder.Services.AddTransient<CriarQuestionarioViewModel>();
			builder.Services.AddTransient<EditarQuestionarioViewModel>();
			builder.Services.AddTransient<QuestionariosPacienteViewModel>();
			builder.Services.AddTransient<ResponderQuestionarioViewModel>();
			builder.Services.AddTransient<DarFeedbackViewModel>();
#if DEBUG
			builder.Logging.AddDebug();
#endif
			return builder.Build();
		}
	}
}