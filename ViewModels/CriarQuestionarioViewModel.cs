using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Services;

namespace MauiApp1.ViewModels
{
	/// <summary>Uma pergunta já montada localmente, esperando o "Salvar"
	/// pra ir tudo junto pro servidor.</summary>
	public class ItemPerguntaForm
	{
		public string Tipo { get; set; } = "Texto"; // "Escala", "Texto" ou "MultiplaEscolha"
		public string Texto { get; set; } = string.Empty;
		public string? Opcoes { get; set; }
		public string Horario { get; set; } = "08:00";

		public string TipoExibido => Tipo switch
		{
			"Escala" => "Escala",
			"MultiplaEscolha" => "Múltipla Escolha",
			_ => "Texto"
		};
	}

	/// <summary>Um paciente na lista de "vincular pacientes" — foto,
	/// nome, se está selecionado, e quais dias da semana as perguntas
	/// devem ser enviadas pra ele (por padrão, todos os 7 dias).</summary>
	public partial class ItemPacienteSelecao : ObservableObject
	{
		public Guid Id { get; set; }
		public string Nome { get; set; } = string.Empty;
		public string? FotoUrl { get; set; }

		public string FotoExibida => string.IsNullOrEmpty(FotoUrl)
			? "avatar_placeholder.jpg"
			: $"{ApiConfig.ServidorBaseUrl}{FotoUrl}";

		[ObservableProperty]
		private bool selecionado;

		// Todos os 7 dias começam marcados por padrão — o psicólogo
		// desmarca só os dias em que NÃO quer que as perguntas cheguem
		// pra esse paciente.
		[ObservableProperty] private bool dom = true;
		[ObservableProperty] private bool seg = true;
		[ObservableProperty] private bool ter = true;
		[ObservableProperty] private bool qua = true;
		[ObservableProperty] private bool qui = true;
		[ObservableProperty] private bool sex = true;
		[ObservableProperty] private bool sab = true;

		/// <summary>Alterna um dia específico — chamado pelo toque no
		/// círculo daquele dia (CommandParameter = "Dom", "Seg", etc).</summary>
		[RelayCommand]
		private void AlternarDia(string dia)
		{
			switch (dia)
			{
				case "Dom": Dom = !Dom; break;
				case "Seg": Seg = !Seg; break;
				case "Ter": Ter = !Ter; break;
				case "Qua": Qua = !Qua; break;
				case "Qui": Qui = !Qui; break;
				case "Sex": Sex = !Sex; break;
				case "Sab": Sab = !Sab; break;
			}
		}

		/// <summary>Monta o CSV pra mandar pra API (ex: "Dom,Seg,Qua").
		/// Se nenhum dia estiver marcado, manda vazio — o domínio trata
		/// vazio como "todos os dias" (ver QuestionarioPaciente), então
		/// isso nunca fica "sem nenhum dia" sem querer.</summary>
		public string DiasSemanaParaApi()
		{
			var dias = new List<string>();
			if (Dom) dias.Add("Dom");
			if (Seg) dias.Add("Seg");
			if (Ter) dias.Add("Ter");
			if (Qua) dias.Add("Qua");
			if (Qui) dias.Add("Qui");
			if (Sex) dias.Add("Sex");
			if (Sab) dias.Add("Sab");
			return string.Join(",", dias);
		}

		/// <summary>Aplica os dias que vieram do servidor (CSV) nos
		/// toggles individuais — usado ao carregar a tela de Editar. Se
		/// vier vazio (paciente ainda sem vínculo, ou algo deu errado),
		/// mantém tudo marcado — é o padrão pedido.</summary>
		public void CarregarDiasSemana(string? diasSemanaCsv)
		{
			var dias = (diasSemanaCsv ?? string.Empty)
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.ToHashSet(StringComparer.OrdinalIgnoreCase);

			if (dias.Count == 0)
			{
				Dom = Seg = Ter = Qua = Qui = Sex = Sab = true;
				return;
			}

			Dom = dias.Contains("Dom");
			Seg = dias.Contains("Seg");
			Ter = dias.Contains("Ter");
			Qua = dias.Contains("Qua");
			Qui = dias.Contains("Qui");
			Sex = dias.Contains("Sex");
			Sab = dias.Contains("Sab");
		}
	}

	public partial class CriarQuestionarioViewModel : ObservableObject
	{
		private readonly QuestionarioApiService _questionarios;
		private readonly VinculoApiService _vinculo;
		private readonly SessaoUsuario _sessao;

		private List<ItemPacienteSelecao> _todosPacientes = new();

		[ObservableProperty]
		private string titulo = string.Empty;

		[ObservableProperty]
		private string tipoPerguntaSelecionado = "Texto";

		[ObservableProperty]
		private string textoPerguntaAtual = string.Empty;

		[ObservableProperty]
		private string opcoesPerguntaAtual = string.Empty;

		[ObservableProperty]
		private string horarioPerguntaAtual = "08:00";

		public bool MostrarCampoOpcoes => TipoPerguntaSelecionado == "MultiplaEscolha";

		partial void OnTipoPerguntaSelecionadoChanged(string value) => OnPropertyChanged(nameof(MostrarCampoOpcoes));

		public ObservableCollection<ItemPerguntaForm> Perguntas { get; } = new();

		[ObservableProperty]
		private string textoBuscaPaciente = string.Empty;

		public ObservableCollection<ItemPacienteSelecao> Pacientes { get; } = new();

		[ObservableProperty]
		private bool carregando;

		[ObservableProperty]
		private string mensagemErro = string.Empty;

		partial void OnTextoBuscaPacienteChanged(string value) => AplicarFiltroPacientes();

		public CriarQuestionarioViewModel(QuestionarioApiService questionarios, VinculoApiService vinculo, SessaoUsuario sessao)
		{
			_questionarios = questionarios;
			_vinculo = vinculo;
			_sessao = sessao;
		}

		[RelayCommand]
		private async Task CarregarAsync()
		{
			// Só pacientes com vínculo ACEITO com esse psicólogo podem
			// ser associados a um questionário dele.
			var vinculos = await _vinculo.ListarPorPsicologoAsync(_sessao.UsuarioId);
			_todosPacientes = vinculos
				.Where(v => v.Status == "Aceito")
				.Select(v => new ItemPacienteSelecao { Id = v.ContatoId, Nome = v.ContatoNome, FotoUrl = v.ContatoFotoUrl })
				.ToList();
			AplicarFiltroPacientes();
		}

		private void AplicarFiltroPacientes()
		{
			Pacientes.Clear();
			var termo = TextoBuscaPaciente?.Trim() ?? string.Empty;

			var filtrados = string.IsNullOrEmpty(termo)
				? _todosPacientes
				: _todosPacientes.Where(p => p.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase));

			foreach (var p in filtrados)
				Pacientes.Add(p);
		}

		[RelayCommand]
		private void SelecionarTipoEscala() => TipoPerguntaSelecionado = "Escala";

		[RelayCommand]
		private void SelecionarTipoTexto() => TipoPerguntaSelecionado = "Texto";

		[RelayCommand]
		private void SelecionarTipoMultiplaEscolha() => TipoPerguntaSelecionado = "MultiplaEscolha";

		[RelayCommand]
		private void AdicionarPergunta()
		{
			if (string.IsNullOrWhiteSpace(TextoPerguntaAtual))
			{
				MensagemErro = "Digite o texto da pergunta.";
				return;
			}

			if (TipoPerguntaSelecionado == "MultiplaEscolha" && string.IsNullOrWhiteSpace(OpcoesPerguntaAtual))
			{
				MensagemErro = "Informe as opções, separadas por vírgula.";
				return;
			}

			if (string.IsNullOrWhiteSpace(HorarioPerguntaAtual))
			{
				MensagemErro = "Informe o horário da notificação.";
				return;
			}

			MensagemErro = string.Empty;

			Perguntas.Add(new ItemPerguntaForm
			{
				Tipo = TipoPerguntaSelecionado,
				Texto = TextoPerguntaAtual.Trim(),
				Opcoes = TipoPerguntaSelecionado == "MultiplaEscolha" ? OpcoesPerguntaAtual.Trim() : null,
				Horario = HorarioPerguntaAtual.Trim()
			});

			TextoPerguntaAtual = string.Empty;
			OpcoesPerguntaAtual = string.Empty;
		}

		[RelayCommand]
		private void RemoverPergunta(ItemPerguntaForm item)
		{
			if (item is not null) Perguntas.Remove(item);
		}

		[RelayCommand]
		private async Task SalvarAsync()
		{
			MensagemErro = string.Empty;

			if (string.IsNullOrWhiteSpace(Titulo))
			{
				MensagemErro = "Digite o título do questionário.";
				return;
			}

			if (Perguntas.Count == 0)
			{
				MensagemErro = "Adicione pelo menos uma pergunta.";
				return;
			}

			Carregando = true;
			try
			{
				var perguntasRequest = Perguntas
					.Select(p => new PerguntaParaCriar(null, p.Tipo, p.Texto, p.Opcoes, p.Horario, true))
					.ToList();

				var pacientesRequest = _todosPacientes
					.Where(p => p.Selecionado)
					.Select(p => new PacienteVinculoParaCriar(p.Id, p.DiasSemanaParaApi()))
					.ToList();

				var (sucesso, _, erro) = await _questionarios.CriarAsync(_sessao.UsuarioId, Titulo, perguntasRequest, pacientesRequest);

				if (!sucesso)
				{
					MensagemErro = erro ?? "Não foi possível salvar o questionário.";
					return;
				}

				await Application.Current!.MainPage!.Navigation.PopAsync();
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível conectar ao servidor: " + ex.Message;
			}
			finally
			{
				Carregando = false;
			}
		}
	}
}