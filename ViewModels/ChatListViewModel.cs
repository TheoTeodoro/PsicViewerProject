using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PsicViewer.Core.Interfaces;
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
		private readonly IPacienteRepository _pacientes;
		private readonly IPsicologoRepository _psicologos;
		private readonly SessaoUsuario _sessao;
		private readonly IServiceProvider _serviceProvider;

		[ObservableProperty]
		private ObservableCollection<ContatoChat> contatos = new();

		[ObservableProperty]
		private bool carregando;

		public ChatListViewModel(
			IPacienteRepository pacientes,
			IPsicologoRepository psicologos,
			SessaoUsuario sessao,
			IServiceProvider serviceProvider)
		{
			_pacientes = pacientes;
			_psicologos = psicologos;
			_sessao = sessao;
			_serviceProvider = serviceProvider;
		}

		[RelayCommand]
		private async Task CarregarAsync()
		{
			Carregando = true;
			try
			{
				Contatos.Clear();

				// NOTA: sem vínculo Paciente-Psicólogo (RF03) ainda, então
				// mostra TODOS do tipo oposto. Quando o vínculo existir,
				// filtrar só pelos vinculados.
				if (_sessao.Tipo == TipoUsuarioLogado.Paciente)
				{
					var psicologos = await _psicologos.ListarTodosAsync();
					foreach (var p in psicologos)
						Contatos.Add(new ContatoChat { Id = p.Id, Nome = p.Nome, Subtitulo = $"CRP {p.Crp}" });
				}
				else if (_sessao.Tipo == TipoUsuarioLogado.Psicologo)
				{
					var pacientes = await _pacientes.ListarTodosAsync();
					foreach (var p in pacientes)
						Contatos.Add(new ContatoChat { Id = p.Id, Nome = p.Nome, Subtitulo = "Paciente" });
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
	}
}