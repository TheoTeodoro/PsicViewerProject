using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Services;

namespace MauiApp1.ViewModels
{
	public partial class ItemPacienteBusca : ObservableObject
	{
		public Guid Id { get; set; }
		public string Nome { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Telefone { get; set; } = string.Empty;

		[ObservableProperty]
		private string status = "Nenhum";

		public string TextoBotao => Status switch
		{
			"Pendente" => "Convite enviado",
			"Aceito" => "Vinculado",
			_ => "Convidar"
		};

		public bool PodeConvidar => Status == "Nenhum" || Status == "Recusado";

		partial void OnStatusChanged(string value)
		{
			OnPropertyChanged(nameof(TextoBotao));
			OnPropertyChanged(nameof(PodeConvidar));
		}
	}

	public partial class BuscarPacienteViewModel : ObservableObject
	{
		private readonly ContaApiService _conta;
		private readonly VinculoApiService _vinculo;
		private readonly SessaoUsuario _sessao;

		private List<ItemPacienteBusca> _todosCarregados = new();

		public ObservableCollection<ItemPacienteBusca> Pacientes { get; } = new();

		[ObservableProperty]
		private bool carregando;

		[ObservableProperty]
		private string mensagemErro = string.Empty;

		[ObservableProperty]
		private string textoBusca = string.Empty;

		partial void OnTextoBuscaChanged(string value) => AplicarFiltro();

		public BuscarPacienteViewModel(ContaApiService conta, VinculoApiService vinculo, SessaoUsuario sessao)
		{
			_conta = conta;
			_vinculo = vinculo;
			_sessao = sessao;
		}

		[RelayCommand]
		private async Task CarregarAsync()
		{
			Carregando = true;
			MensagemErro = string.Empty;
			try
			{
				var todos = await _conta.ListarPacientesAsync();
				var meusVinculos = await _vinculo.ListarPorPsicologoAsync(_sessao.UsuarioId);

				_todosCarregados = todos.Select(p =>
				{
					var vinculoExistente = meusVinculos
						.Where(v => v.ContatoId == p.Id)
						.OrderByDescending(v => v.SolicitadoEm)
						.FirstOrDefault();

					return new ItemPacienteBusca
					{
						Id = p.Id,
						Nome = p.Nome,
						Email = p.Email,
						Telefone = p.Telefone ?? string.Empty,
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
			Pacientes.Clear();

			var termo = TextoBusca?.Trim() ?? string.Empty;
			var filtrados = string.IsNullOrEmpty(termo)
				? _todosCarregados
				: _todosCarregados.Where(p =>
					p.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
					p.Email.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
					p.Telefone.Contains(termo, StringComparison.OrdinalIgnoreCase));

			foreach (var p in filtrados)
				Pacientes.Add(p);
		}

		[RelayCommand]
		private async Task ConvidarAsync(ItemPacienteBusca item)
		{
			if (item is null || !item.PodeConvidar) return;

			var (sucesso, erro) = await _vinculo.SolicitarComoPsicologoAsync(_sessao.UsuarioId, item.Id);
			if (!sucesso)
			{
				MensagemErro = erro ?? "Não foi possível enviar o convite.";
				return;
			}

			item.Status = "Pendente";
		}
	}
}