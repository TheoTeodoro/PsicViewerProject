using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Services;

namespace MauiApp1.ViewModels
{
	/// <summary>Uma pergunta já existente no questionário, editável: dá
	/// pra mudar o horário e ativar/desativar, mas não o texto/tipo (pra
	/// isso, desativa essa e cria uma nova).</summary>
	public partial class ItemPerguntaExistente : ObservableObject
	{
		public Guid? Id { get; set; }
		public string Tipo { get; set; } = "Texto";
		public string Texto { get; set; } = string.Empty;
		public string? Opcoes { get; set; }

		[ObservableProperty]
		private string horario = "08:00";

		[ObservableProperty]
		private bool ativa = true;

		public string TipoExibido => Tipo switch
		{
			"Escala" => "Escala",
			"MultiplaEscolha" => "Múltipla Escolha",
			_ => "Texto"
		};

		/// <summary>Deixa o card visualmente "apagado" quando a pergunta
		/// está desativada — deixa óbvio que ela não está sendo enviada.</summary>
		public double OpacidadeCard => Ativa ? 1.0 : 0.5;

		partial void OnAtivaChanged(bool value) => OnPropertyChanged(nameof(OpacidadeCard));
	}

	public partial class EditarQuestionarioViewModel : ObservableObject
	{
		private readonly QuestionarioApiService _questionarios;
		private readonly VinculoApiService _vinculo;
		private readonly SessaoUsuario _sessao;

		private Guid _questionarioId;
		private string _tituloOriginal = string.Empty;
		private List<ItemPacienteSelecao> _todosPacientes = new();

		[ObservableProperty]
		private string titulo = string.Empty;

		public ObservableCollection<ItemPerguntaExistente> Perguntas { get; } = new();

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

		[ObservableProperty]
		private string textoBuscaPaciente = string.Empty;

		public ObservableCollection<ItemPacienteSelecao> Pacientes { get; } = new();

		[ObservableProperty]
		private bool carregando;

		// Trava contra duplo toque: os botões "Salvar Alterações" e
		// "Criar Novo Questionário" ficam desabilitados enquanto uma
		// chamada já está em andamento. Sem isso, dois toques rápidos
		// disparavam duas requisições PUT concorrentes — a segunda
		// carregava o Questionário um instante antes/depois da primeira
		// terminar de salvar, e tentava dar UPDATE numa Pergunta cujo
		// estado já tinha mudado por causa da primeira, gerando
		// DbUpdateConcurrencyException (erro 500) no servidor.
		public bool PodeSalvar => !Carregando;

		partial void OnCarregandoChanged(bool value) => OnPropertyChanged(nameof(PodeSalvar));

		[ObservableProperty]
		private string mensagemErro = string.Empty;

		partial void OnTextoBuscaPacienteChanged(string value) => AplicarFiltroPacientes();

		public EditarQuestionarioViewModel(QuestionarioApiService questionarios, VinculoApiService vinculo, SessaoUsuario sessao)
		{
			_questionarios = questionarios;
			_vinculo = vinculo;
			_sessao = sessao;
		}

		public async Task CarregarAsync(Guid questionarioId)
		{
			Carregando = true;
			MensagemErro = string.Empty;
			try
			{
				_questionarioId = questionarioId;
				var detalhe = await _questionarios.ObterParaEditarAsync(questionarioId);

				if (detalhe is null)
				{
					MensagemErro = "Não foi possível carregar esse questionário.";
					return;
				}

				Titulo = detalhe.Titulo;
				_tituloOriginal = detalhe.Titulo;

				Perguntas.Clear();
				foreach (var p in detalhe.Perguntas)
				{
					Perguntas.Add(new ItemPerguntaExistente
					{
						Id = p.Id,
						Tipo = p.Tipo,
						Texto = p.Texto,
						Opcoes = p.Opcoes,
						Horario = p.Horario,
						Ativa = p.Ativa
					});
				}

				// Só pacientes com vínculo ACEITO com esse psicólogo podem
				// continuar (ou passar a) ser associados a esse questionário.
				var vinculos = await _vinculo.ListarPorPsicologoAsync(_sessao.UsuarioId);
				var vinculados = detalhe.Pacientes.ToDictionary(x => x.PacienteId, x => x.DiasSemana);

				_todosPacientes = vinculos
					.Where(v => v.Status == "Aceito")
					.Select(v =>
					{
						var item = new ItemPacienteSelecao
						{
							Id = v.ContatoId,
							Nome = v.ContatoNome,
							FotoUrl = v.ContatoFotoUrl,
							Selecionado = vinculados.ContainsKey(v.ContatoId)
						};
						item.CarregarDiasSemana(vinculados.TryGetValue(v.ContatoId, out var dias) ? dias : null);
						return item;
					}).ToList();

				AplicarFiltroPacientes();
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

			Perguntas.Add(new ItemPerguntaExistente
			{
				Id = null,
				Tipo = TipoPerguntaSelecionado,
				Texto = TextoPerguntaAtual.Trim(),
				Opcoes = TipoPerguntaSelecionado == "MultiplaEscolha" ? OpcoesPerguntaAtual.Trim() : null,
				Horario = HorarioPerguntaAtual.Trim(),
				Ativa = true
			});

			TextoPerguntaAtual = string.Empty;
			OpcoesPerguntaAtual = string.Empty;
		}

		[RelayCommand]
		private async Task RemoverPerguntaAsync(ItemPerguntaExistente? pergunta)
		{
			if (pergunta is null) return;

			var confirmar = await Application.Current!.MainPage!.DisplayAlert(
				"Remover pergunta",
				"Tem certeza que deseja remover essa pergunta? Essa ação não pode ser desfeita.",
				"Remover", "Cancelar");

			if (!confirmar) return;

			Perguntas.Remove(pergunta);

			// Se essa pergunta já tiver respostas registradas, o servidor
			// recusa a remoção ao salvar (preferimos perder a tentativa de
			// exclusão a perder histórico clínico por engano) — a mensagem
			// de erro aparece normalmente em MensagemErro nesse caso.
		}

		[RelayCommand]
		private async Task SalvarEdicaoAsync() => await SalvarInternoAsync(criarNovo: false);

		[RelayCommand]
		private async Task CriarNovoAsync() => await SalvarInternoAsync(criarNovo: true);

		private async Task SalvarInternoAsync(bool criarNovo)
		{
			// Guard contra duplo toque: se já tem uma chamada em
			// andamento (Carregando == true), ignora este segundo
			// disparo em vez de mandar outra requisição por cima.
			// Funciona porque Carregando é setado como true de forma
			// síncrona, antes do primeiro "await" — então mesmo que o
			// segundo toque chegue enquanto a primeira chamada ainda
			// está em voo, ele já encontra a trava ligada.
			if (Carregando) return;

			MensagemErro = string.Empty;

			if (string.IsNullOrWhiteSpace(Titulo))
			{
				MensagemErro = "Digite o título do questionário.";
				return;
			}

			if (Perguntas.Count == 0)
			{
				MensagemErro = "O questionário precisa ter pelo menos uma pergunta.";
				return;
			}

			Carregando = true;
			try
			{
				var perguntasRequest = Perguntas
					.Select(p => new PerguntaParaCriar(criarNovo ? null : p.Id, p.Tipo, p.Texto, p.Opcoes, p.Horario, p.Ativa))
					.ToList();

				var pacientesRequest = _todosPacientes
					.Where(p => p.Selecionado)
					.Select(p => new PacienteVinculoParaCriar(p.Id, p.DiasSemanaParaApi()))
					.ToList();

				if (criarNovo)
				{
					// Se o título não foi alterado, gera um nome automático
					// pra diferenciar da cópia original.
					var tituloFinal = Titulo.Trim().Equals(_tituloOriginal.Trim(), StringComparison.OrdinalIgnoreCase)
						? $"{Titulo.Trim()} (cópia)"
						: Titulo.Trim();

					var (sucesso, _, erro) = await _questionarios.CriarAsync(_sessao.UsuarioId, tituloFinal, perguntasRequest, pacientesRequest);
					if (!sucesso)
					{
						MensagemErro = erro ?? "Não foi possível criar o novo questionário.";
						return;
					}
				}
				else
				{
					var (sucesso, erro) = await _questionarios.EditarAsync(_questionarioId, Titulo.Trim(), perguntasRequest, pacientesRequest);
					if (!sucesso)
					{
						MensagemErro = erro ?? "Não foi possível salvar as alterações.";
						return;
					}
				}

				// Sai da tela de Editar Questionário e volta pra página
				// anterior — SEMPRE que Salvar Alterações ou Criar Novo
				// tiverem sucesso. Não remover.
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

		[RelayCommand]
		private async Task ExcluirQuestionarioAsync()
		{
			var confirmar = await Application.Current!.MainPage!.DisplayAlert(
				"Apagar questionário",
				"Tem certeza que deseja apagar esse questionário? Isso remove todas as perguntas e respostas registradas dele. Essa ação não pode ser desfeita.",
				"Apagar", "Cancelar");

			if (!confirmar) return;

			Carregando = true;
			try
			{
				var (sucesso, erro) = await _questionarios.ExcluirAsync(_questionarioId);
				if (!sucesso)
				{
					MensagemErro = erro ?? "Não foi possível apagar o questionário.";
					return;
				}

				await Application.Current!.MainPage!.Navigation.PopAsync();
			}
			finally
			{
				Carregando = false;
			}
		}

		[RelayCommand]
		private async Task VoltarAsync() => await Application.Current!.MainPage!.Navigation.PopAsync();
	}
}