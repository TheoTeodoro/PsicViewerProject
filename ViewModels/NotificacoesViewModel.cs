using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MauiApp1.Services;
using MauiApp1.Views;
namespace MauiApp1.ViewModels
{
	public partial class NotificacoesViewModel : ObservableObject
	{
		private readonly NotificacaoService _notificacoes;
		private readonly IServiceProvider _serviceProvider;
		private readonly SessaoUsuario _sessao;
		public ObservableCollection<ItemNotificacao> Itens { get; } = new();
		[ObservableProperty]
		private bool carregando;
		[ObservableProperty]
		private string resumo = "Nenhuma notificação pendente";
		public NotificacoesViewModel(NotificacaoService notificacoes, IServiceProvider serviceProvider, SessaoUsuario sessao)
		{
			_notificacoes = notificacoes;
			_serviceProvider = serviceProvider;
			_sessao = sessao;
		}
		[RelayCommand]
		private async Task CarregarAsync()
		{
			Carregando = true;
			try
			{
				var itens = await _notificacoes.ObterNotificacoesAsync();
				var naoVisualizadas = itens.Count(i => i.NaoLida);
				Resumo = naoVisualizadas > 0
					? $"{naoVisualizadas} não visualizada{(naoVisualizadas > 1 ? "s" : string.Empty)}"
					: "Nenhuma notificação pendente";
				Itens.Clear();
				foreach (var item in itens)
					Itens.Add(item);
				await _notificacoes.MarcarTodasComoVistasAsync(itens);

				 
				_notificacoes.MarcarListaComoVisualizadaAgora();
			}
			finally
			{
				Carregando = false;
			}
		}

		 
		[RelayCommand]
		private async Task LimparNotificacoesAsync()
		{
			if (Itens.Count == 0) return;

			var confirmar = await Application.Current!.MainPage!.DisplayAlert(
				"Limpar notificações",
				"Tem certeza que quer limpar todas as notificações?",
				"Limpar", "Cancelar");

			if (!confirmar) return;

			Carregando = true;
			try
			{
				await _notificacoes.LimparTodasAsync(Itens);
				Itens.Clear();
				Resumo = "Nenhuma notificação pendente";
			}
			finally
			{
				Carregando = false;
			}
		}

		[RelayCommand]
		private async Task AbrirItemAsync(ItemNotificacao item)
		{
			if (item is null) return;

			if (item.Tipo == TipoNotificacao.SolicitacaoVinculo)
			{
				if (_sessao.Tipo == TipoUsuarioLogado.Paciente)
				{
					var page = _serviceProvider.GetRequiredService<BuscarPsicologoPage>();
					await Application.Current!.MainPage!.Navigation.PushAsync(page);
				}
				else
				{
					var page = _serviceProvider.GetRequiredService<PacientesPage>();
					await Application.Current!.MainPage!.Navigation.PushAsync(page);
				}
				return;
			}

			if (item.Tipo == TipoNotificacao.RespostaQuestionario)
			{
				 
				var page = _serviceProvider.GetRequiredService<DarFeedbackPage>();
				if (page.BindingContext is DarFeedbackViewModel vm)
					await vm.CarregarAsync(item.VinculoId);
				await Application.Current!.MainPage!.Navigation.PushAsync(page);
				return;
			}
 
			var chatPage = _serviceProvider.GetRequiredService<ChatConversaPage>();
			if (chatPage.BindingContext is ChatConversaViewModel vmChat)
				await vmChat.DefinirContatoAsync(item.ContatoId, item.ContatoNome);
			await Application.Current!.MainPage!.Navigation.PushAsync(chatPage);
		}
	}
}