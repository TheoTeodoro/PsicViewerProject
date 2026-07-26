using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MauiApp1.Services;
using MauiApp1.Views;
namespace MauiApp1.ViewModels
{
	public class ContatoChat
	{
		public Guid Id { get; set; }
		public string Nome { get; set; } = string.Empty;
		public string Subtitulo { get; set; } = string.Empty;
	}
	public partial class ChatListViewModel : ObservableObject
	{
		private readonly VinculoApiService _vinculo;
		private readonly SessaoUsuario _sessao;
		private readonly IServiceProvider _serviceProvider;
		[ObservableProperty]
		private ObservableCollection<ContatoChat> contatos = new();
		[ObservableProperty]
		private bool carregando;
		public bool EhPaciente => _sessao.Tipo == TipoUsuarioLogado.Paciente;
		public bool EhPsicologo => _sessao.Tipo == TipoUsuarioLogado.Psicologo;
		public ChatListViewModel(VinculoApiService vinculo, SessaoUsuario sessao, IServiceProvider serviceProvider)
		{
			_vinculo = vinculo;
			_sessao = sessao;
			_serviceProvider = serviceProvider;
		}
		[RelayCommand]
		private async Task CarregarAsync()
		{
			// Trava contra chamada concorrente: no Android, OnAppearing pode
			// disparar 2x na primeira vez que a página é empilhada. Sem essa
			// trava, as duas chamadas assíncronas rodavam em paralelo — cada
			// uma dava Clear() e depois adicionava a lista inteira de novo,
			// duplicando os primeiros contatos quando as duas terminavam de
			// buscar quase ao mesmo tempo. Na tela seguinte (ou ao atualizar),
			// só uma chamada roda por vez, por isso o problema sumia.
			if (Carregando) return;

			Carregando = true;
			try
			{
				Contatos.Clear();
				// Agora só mostra quem tem vínculo ACEITO (RF03) — antes
				// mostrava todo mundo do tipo oposto, sem filtro nenhum.
				if (_sessao.Tipo == TipoUsuarioLogado.Paciente)
				{
					var vinculos = await _vinculo.ListarPorPacienteAsync(_sessao.UsuarioId);
					foreach (var v in vinculos.Where(v => v.Status == "Aceito"))
						Contatos.Add(new ContatoChat { Id = v.ContatoId, Nome = v.ContatoNome, Subtitulo = $"CRP {v.ContatoCrp}" });
				}
				else if (_sessao.Tipo == TipoUsuarioLogado.Psicologo)
				{
					var vinculos = await _vinculo.ListarPorPsicologoAsync(_sessao.UsuarioId);
					foreach (var v in vinculos.Where(v => v.Status == "Aceito"))
						Contatos.Add(new ContatoChat { Id = v.ContatoId, Nome = v.ContatoNome, Subtitulo = "Paciente" });
				}
			}
			finally
			{
				Carregando = false;
			}
		}
		[RelayCommand]
		private async Task AbrirConversaAsync(ContatoChat contato)
		{
			if (contato is null) return;
			var page = _serviceProvider.GetRequiredService<ChatConversaPage>();
			if (page.BindingContext is ChatConversaViewModel vm)
			{
				await vm.DefinirContatoAsync(contato.Id, contato.Nome);
			}
			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}
		[RelayCommand]
		private async Task AbrirBuscarPsicologoAsync()
		{
			var page = _serviceProvider.GetRequiredService<BuscarPsicologoPage>();
			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}
		[RelayCommand]
		private async Task AbrirPacientesAsync()
		{
			var page = _serviceProvider.GetRequiredService<PacientesPage>();
			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}
	}
}