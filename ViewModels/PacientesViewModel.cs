using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MauiApp1.Services;
using MauiApp1.Views;

namespace MauiApp1.ViewModels
{
	public class ItemVinculo
	{
		public Guid VinculoId { get; set; }

		/// <summary>Id do PACIENTE em si (diferente do VinculoId, que é o
		/// Id do vínculo) — precisa disso pra abrir os dados dele.</summary>
		public Guid PacienteId { get; set; }
		public string Nome { get; set; } = string.Empty;
		public string? FotoUrl { get; set; }

		public string FotoExibida => string.IsNullOrEmpty(FotoUrl)
			? "avatar_placeholder.jpg"
			: $"{ApiConfig.ServidorBaseUrl}{FotoUrl}";
	}

	public partial class PacientesViewModel : ObservableObject
	{
		private readonly VinculoApiService _vinculo;
		private readonly SessaoUsuario _sessao;
		private readonly IServiceProvider _serviceProvider;

		/// <summary>Paciente solicitou, eu (psicólogo) preciso responder.</summary>
		public ObservableCollection<ItemVinculo> SolicitacoesRecebidas { get; } = new();

		/// <summary>Eu convidei o paciente, esperando ele responder.</summary>
		public ObservableCollection<ItemVinculo> ConvitesEnviados { get; } = new();

		public ObservableCollection<ItemVinculo> Vinculados { get; } = new();

		[ObservableProperty]
		private bool carregando;

		[ObservableProperty]
		private string mensagemErro = string.Empty;

		public PacientesViewModel(VinculoApiService vinculo, SessaoUsuario sessao, IServiceProvider serviceProvider)
		{
			_vinculo = vinculo;
			_sessao = sessao;
			_serviceProvider = serviceProvider;
		}

		[RelayCommand]
		private async Task CarregarAsync()
		{
			Carregando = true;
			MensagemErro = string.Empty;
			try
			{
				var lista = await _vinculo.ListarPorPsicologoAsync(_sessao.UsuarioId);

				SolicitacoesRecebidas.Clear();
				ConvitesEnviados.Clear();
				Vinculados.Clear();

				foreach (var v in lista)
				{
					var item = new ItemVinculo { VinculoId = v.Id, PacienteId = v.ContatoId, Nome = v.ContatoNome, FotoUrl = v.ContatoFotoUrl };

					if (v.Status == "Aceito")
					{
						Vinculados.Add(item);
					}
					else if (v.Status == "Pendente" && v.Origem == "Paciente")
					{
						// O paciente que solicitou — eu preciso aceitar/recusar.
						SolicitacoesRecebidas.Add(item);
					}
					else if (v.Status == "Pendente" && v.Origem == "Psicologo")
					{
						// Eu que convidei — só aguardando o paciente responder.
						ConvitesEnviados.Add(item);
					}
				}
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível carregar: " + ex.Message;
			}
			finally
			{
				Carregando = false;
			}
		}

		[RelayCommand]
		private async Task AceitarAsync(ItemVinculo item)
		{
			if (item is null) return;

			var ok = await _vinculo.AceitarAsync(item.VinculoId);
			if (ok)
				await CarregarAsync();
			else
				MensagemErro = "Não foi possível aceitar a solicitação.";
		}

		[RelayCommand]
		private async Task RecusarAsync(ItemVinculo item)
		{
			if (item is null) return;

			var ok = await _vinculo.RecusarAsync(item.VinculoId);
			if (ok)
				await CarregarAsync();
			else
				MensagemErro = "Não foi possível recusar a solicitação.";
		}

		/// <summary>Abre os dados públicos do paciente — só faz sentido pra
		/// quem já está de fato vinculado (não pra solicitações pendentes).</summary>
		[RelayCommand]
		private async Task AbrirPacienteAsync(ItemVinculo item)
		{
			if (item is null) return;

			var page = _serviceProvider.GetRequiredService<DadosPacientePage>();
			if (page.BindingContext is DadosPacienteViewModel vm)
				await vm.CarregarAsync(item.PacienteId);

			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}

		/// <summary>Histórico de questionários respondidos por esse paciente
		/// — só os que ESTE psicólogo mesmo vinculou a ele (o servidor
		/// garante isso, não depende do app).</summary>
		[RelayCommand]
		private async Task AbrirHistoricoAsync(ItemVinculo item)
		{
			if (item is null) return;

			var page = _serviceProvider.GetRequiredService<HistoricoPacientePage>();
			if (page.BindingContext is HistoricoPacienteViewModel vm)
				await vm.CarregarAsync(item.PacienteId, item.Nome);

			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}

		[RelayCommand]
		private async Task AbrirBuscarPacienteAsync()
		{
			var page = _serviceProvider.GetRequiredService<BuscarPacientePage>();
			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}
	}
}
