using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MauiApp1.Services;
using MauiApp1.Views;
namespace MauiApp1.ViewModels
{
	/// <summary>Um questionário pendente hoje, com o progresso de quantas
	/// perguntas já foram respondidas.</summary>
	public class ItemQuestionarioPendente
	{
		public Guid Id { get; set; }
		public string Titulo { get; set; } = string.Empty;
		public int QuantidadePerguntas { get; set; }
		public int QuantidadeRespondidasHoje { get; set; }

		public string ProgressoExibido => $"{QuantidadeRespondidasHoje}/{QuantidadePerguntas} respondidas hoje";
		public bool CompletoHoje => QuantidadePerguntas > 0 && QuantidadeRespondidasHoje >= QuantidadePerguntas;
	}

	/// <summary>Uma resposta já arquivada no Histórico (dia anterior a
	/// hoje), com o texto formatado pronto pra exibir.</summary>
	public class ItemHistorico
	{
		public Guid RespostaId { get; set; }
		public string QuestionarioTitulo { get; set; } = string.Empty;
		public string PerguntaTexto { get; set; } = string.Empty;
		public DateTime RespondidoEm { get; set; }
		public string RespostaResumo { get; set; } = string.Empty;

		public string HorarioExibido => RespondidoEm.ToString("HH:mm");
	}

	/// <summary>Um grupo de itens de histórico no mesmo dia — cabeçalho
	/// tipo "Segunda, 24 de fevereiro".</summary>
	public class GrupoHistorico : List<ItemHistorico>
	{
		public string NomeGrupo { get; }
		public GrupoHistorico(string nomeGrupo, List<ItemHistorico> itens) : base(itens)
		{
			NomeGrupo = nomeGrupo;
		}
	}

	/// <summary>Um questionário respondido num dia específico — é isso
	/// que aparece na lista de Histórico do PACIENTE (agrupado por
	/// questionário+dia, não pergunta por pergunta) e é clicável: toca
	/// pra ver as perguntas e respostas daquele dia específico.</summary>
	public class ItemHistoricoQuestionario
	{
		public Guid QuestionarioId { get; set; }
		public string QuestionarioTitulo { get; set; } = string.Empty;
		public DateTime UltimaRespostaEm { get; set; }
		public string DataParaApi { get; set; } = string.Empty; // "yyyy-MM-dd"

		public string HorarioExibido => UltimaRespostaEm.ToString("HH:mm");
	}

	/// <summary>Um grupo de questionários respondidos no mesmo dia.</summary>
	public class GrupoHistoricoQuestionario : List<ItemHistoricoQuestionario>
	{
		public string NomeGrupo { get; }
		public GrupoHistoricoQuestionario(string nomeGrupo, List<ItemHistoricoQuestionario> itens) : base(itens)
		{
			NomeGrupo = nomeGrupo;
		}
	}

	public partial class QuestionariosPacienteViewModel : ObservableObject
	{
		private readonly QuestionarioApiService _questionarios;
		private readonly SessaoUsuario _sessao;
		private readonly IServiceProvider _serviceProvider;

		public ObservableCollection<ItemQuestionarioPendente> Questionarios { get; } = new();
		public ObservableCollection<GrupoHistoricoQuestionario> GruposHistorico { get; } = new();

		[ObservableProperty]
		private bool carregando;

		[ObservableProperty]
		private string mensagemErro = string.Empty;

		[ObservableProperty]
		private string filtroSelecionado = "EmUso";

		public Color CorFundoEmUso => FiltroSelecionado == "EmUso" ? Color.FromArgb("#004AAD") : Colors.White;
		public Color CorFundoHistorico => FiltroSelecionado == "Histórico" ? Color.FromArgb("#004AAD") : Colors.White;
		public Color CorTextoEmUso => FiltroSelecionado == "EmUso"
			? Colors.White
			: (Application.Current?.Resources.TryGetValue("AzulEscuro", out var corEmUso) == true ? (Color)corEmUso : Colors.Black);
		public Color CorTextoHistorico => FiltroSelecionado == "Histórico"
			? Colors.White
			: (Application.Current?.Resources.TryGetValue("AzulEscuro", out var corHistorico) == true ? (Color)corHistorico : Colors.Black);

		partial void OnFiltroSelecionadoChanged(string value)
		{
			OnPropertyChanged(nameof(CorFundoEmUso));
			OnPropertyChanged(nameof(CorFundoHistorico));
			OnPropertyChanged(nameof(CorTextoEmUso));
			OnPropertyChanged(nameof(CorTextoHistorico));
		}

		private bool _historicoJaCarregado;

		public string FotoExibida => string.IsNullOrEmpty(_sessao.FotoUrl)
			? "avatar_placeholder.jpg"
			: $"{ApiConfig.ServidorBaseUrl}{_sessao.FotoUrl}";

		public QuestionariosPacienteViewModel(QuestionarioApiService questionarios, SessaoUsuario sessao, IServiceProvider serviceProvider)
		{
			_questionarios = questionarios;
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
				var lista = await _questionarios.ListarPorPacienteAsync(_sessao.UsuarioId);
				Questionarios.Clear();
				foreach (var q in lista)
				{
					Questionarios.Add(new ItemQuestionarioPendente
					{
						Id = q.Id,
						Titulo = q.Titulo,
						QuantidadePerguntas = q.QuantidadePerguntas,
						QuantidadeRespondidasHoje = q.QuantidadeRespondidasHoje
					});
				}

				// Histórico só é buscado quando o paciente troca pra essa
				// aba (ou já tiver sido carregado antes) — evita uma
				// chamada extra toda vez que só quer ver os pendentes.
				if (FiltroSelecionado == "Histórico" || _historicoJaCarregado)
					await CarregarHistoricoAsync();
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível carregar seus questionários: " + ex.Message;
			}
			finally
			{
				Carregando = false;
			}
		}

		private async Task CarregarHistoricoAsync()
		{
			var historico = await _questionarios.ListarHistoricoAsync(_sessao.UsuarioId);
			_historicoJaCarregado = true;

			// Agrupa por questionário+dia — uma "submissão" — em vez de
			// uma linha por pergunta respondida. Cada submissão é um item
			// clicável na lista; o detalhe (pergunta a pergunta) só é
			// buscado quando o paciente toca nela.
			var submissoes = historico
				.GroupBy(h => (h.QuestionarioId, h.Data))
				.Select(g => new ItemHistoricoQuestionario
				{
					QuestionarioId = g.Key.QuestionarioId,
					QuestionarioTitulo = g.First().QuestionarioTitulo,
					UltimaRespostaEm = g.Max(h => h.RespondidoEm),
					DataParaApi = g.Key.Data
				})
				.ToList();

			var agrupados = submissoes
				.GroupBy(i => i.UltimaRespostaEm.Date)
				.OrderByDescending(g => g.Key)
				.Select(g => new GrupoHistoricoQuestionario(
					CapitalizarPrimeiraLetra(g.Key.ToString("dddd, dd 'de' MMMM", new CultureInfo("pt-BR"))),
					g.OrderByDescending(i => i.UltimaRespostaEm).ToList()));

			GruposHistorico.Clear();
			foreach (var grupo in agrupados)
				GruposHistorico.Add(grupo);
		}

		private static string CapitalizarPrimeiraLetra(string texto)
			=> string.IsNullOrEmpty(texto) ? texto : char.ToUpper(texto[0], new CultureInfo("pt-BR")) + texto[1..];

		[RelayCommand]
		private void FiltrarEmUso() => FiltroSelecionado = "EmUso";

		[RelayCommand]
		private async Task FiltrarHistoricoAsync()
		{
			FiltroSelecionado = "Histórico";
			if (!_historicoJaCarregado)
				await CarregarHistoricoAsync();
		}

		[RelayCommand]
		private async Task ResponderAsync(ItemQuestionarioPendente item)
		{
			if (item is null) return;
			var page = _serviceProvider.GetRequiredService<ResponderQuestionarioPage>();
			if (page.BindingContext is ResponderQuestionarioViewModel vm)
				await vm.CarregarAsync(item.Id);
			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}

	
		[RelayCommand]
		private async Task AbrirSubmissaoHistoricoAsync(ItemHistoricoQuestionario item)
		{
			if (item is null) return;

			var page = _serviceProvider.GetRequiredService<DetalheHistoricoQuestionarioPage>();
			if (page.BindingContext is DetalheHistoricoQuestionarioViewModel vm)
				await vm.CarregarAsync(item.QuestionarioId, _sessao.UsuarioId, item.DataParaApi, item.QuestionarioTitulo);

			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}

		[RelayCommand]
		private async Task IrParaHomeAsync()
		{
			await Application.Current!.MainPage!.Navigation.PopToRootAsync();
		}

		[RelayCommand]
		private async Task AbrirChatAsync()
		{
			await Application.Current!.MainPage!.Navigation.PopToRootAsync();
			var page = _serviceProvider.GetRequiredService<ChatListPage>();
			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}

		[RelayCommand]
		private async Task AbrirPerfilAsync()
		{
			var page = _serviceProvider.GetRequiredService<PerfilPage>();
			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}
	}
}
