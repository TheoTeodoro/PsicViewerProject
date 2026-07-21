using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MauiApp1.Services;
using MauiApp1.Views;

namespace MauiApp1.ViewModels
{
	public partial class ItemPsicologoBusca : ObservableObject
	{
		public Guid Id { get; set; }
		public string Nome { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Crp { get; set; } = string.Empty;

		[ObservableProperty]
		private string status = "Nenhum"; // "Nenhum", "Pendente", "Aceito", "Recusado"

		public string TextoBotao => Status switch
		{
			"Pendente" => "Pendente",
			"Aceito" => "Vinculado",
			_ => "Solicitar"
		};

		public bool PodeSolicitar => Status == "Nenhum" || Status == "Recusado";

		partial void OnStatusChanged(string value)
		{
			OnPropertyChanged(nameof(TextoBotao));
			OnPropertyChanged(nameof(PodeSolicitar));
		}
	}

	public partial class BuscarPsicologoViewModel : ObservableObject
	{
		private readonly ContaApiService _conta;
		private readonly VinculoApiService _vinculo;
		private readonly SessaoUsuario _sessao;
		private readonly IServiceProvider _serviceProvider;

		private List<ItemPsicologoBusca> _todosCarregados = new();

		public ObservableCollection<ItemPsicologoBusca> Psicologos { get; } = new();

		[ObservableProperty]
		private bool carregando;

		[ObservableProperty]
		private string mensagemErro = string.Empty;

		[ObservableProperty]
		private string textoBusca = string.Empty;

		// Se um psicólogo te convidou (origem = Psicologo, pendente),
		// isso guarda esse vínculo pra mostrar o banner de notificação.
		[ObservableProperty]
		private VinculoDto? convitePendente;

		public bool TemConvitePendente => ConvitePendente is not null;

		partial void OnConvitePendenteChanged(VinculoDto? value) => OnPropertyChanged(nameof(TemConvitePendente));

		partial void OnTextoBuscaChanged(string value) => AplicarFiltro();

		public BuscarPsicologoViewModel(ContaApiService conta, VinculoApiService vinculo, SessaoUsuario sessao, IServiceProvider serviceProvider)
		{
			_conta = conta;
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
				var todos = await _conta.ListarPsicologosAsync();
				var meusVinculos = await _vinculo.ListarPorPacienteAsync(_sessao.UsuarioId);

				// Convite recebido: alguém (psicólogo) solicitou vínculo
				// com esse paciente e ainda está pendente de resposta.
				ConvitePendente = meusVinculos.FirstOrDefault(v => v.Origem == "Psicologo" && v.Status == "Pendente");

				_todosCarregados = todos.Select(p =>
				{
					var vinculoExistente = meusVinculos
						.Where(v => v.ContatoId == p.Id)
						.OrderByDescending(v => v.SolicitadoEm)
						.FirstOrDefault();

					return new ItemPsicologoBusca
					{
						Id = p.Id,
						Nome = p.Nome,
						Email = p.Email,
						Crp = p.Crp ?? string.Empty,
						Status = vinculoExistente?.Status ?? "Nenhum"
					};
				}).ToList();

				AplicarFiltro();
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

		private void AplicarFiltro()
		{
			Psicologos.Clear();

			var termo = TextoBusca?.Trim() ?? string.Empty;
			var filtrados = string.IsNullOrEmpty(termo)
				? _todosCarregados
				: _todosCarregados.Where(p =>
					p.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
					p.Crp.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
					p.Email.Contains(termo, StringComparison.OrdinalIgnoreCase));

			foreach (var p in filtrados)
				Psicologos.Add(p);
		}

		[RelayCommand]
		private async Task SolicitarAsync(ItemPsicologoBusca item)
		{
			if (item is null || !item.PodeSolicitar) return;

			var (sucesso, erro) = await _vinculo.SolicitarAsync(_sessao.UsuarioId, item.Id);
			if (!sucesso)
			{
				MensagemErro = erro ?? "Não foi possível solicitar o vínculo.";
				return;
			}

			item.Status = "Pendente";
		}

		[RelayCommand]
		private async Task AbrirConviteAsync()
		{
			if (ConvitePendente is null) return;

			var page = _serviceProvider.GetRequiredService<SolicitacaoVinculoPage>();
			if (page.BindingContext is SolicitacaoVinculoViewModel vm)
				vm.Definir(ConvitePendente);

			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}
	}
}