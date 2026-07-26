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

	public partial class QuestionariosPacienteViewModel : ObservableObject
	{
		private readonly QuestionarioApiService _questionarios;
		private readonly SessaoUsuario _sessao;
		private readonly IServiceProvider _serviceProvider;

		public ObservableCollection<ItemQuestionarioPendente> Questionarios { get; } = new();
		public ObservableCollection<GrupoHistorico> GruposHistorico { get; } = new();

		[ObservableProperty]
		private bool carregando;

		[ObservableProperty]
		private string mensagemErro = string.Empty;

		[ObservableProperty]
		private string filtroSelecionado = "Pendentes";

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

			var itens = historico.Select(h => new ItemHistorico
			{
				RespostaId = h.RespostaId,
				QuestionarioTitulo = h.QuestionarioTitulo,
				PerguntaTexto = h.PerguntaTexto,
				RespondidoEm = h.RespondidoEm,
				RespostaResumo = FormatarResposta(h)
			}).ToList();

			var agrupados = itens
				.GroupBy(i => i.RespondidoEm.Date)
				.OrderByDescending(g => g.Key)
				.Select(g => new GrupoHistorico(
					CapitalizarPrimeiraLetra(g.Key.ToString("dddd, dd 'de' MMMM", new CultureInfo("pt-BR"))),
					g.OrderByDescending(i => i.RespondidoEm).ToList()));

			GruposHistorico.Clear();
			foreach (var grupo in agrupados)
				GruposHistorico.Add(grupo);
		}

		private static string FormatarResposta(ItemHistoricoDto h)
		{
			var partes = new List<string>();

			if (!string.IsNullOrWhiteSpace(h.RespostaTexto))
				partes.Add(h.RespostaTexto);
			else if (h.ValorEscala is int valor)
				partes.Add($"Nível {valor}");

			if (!string.IsNullOrWhiteSpace(h.Observacao))
				partes.Add($"Observação: {h.Observacao}");

			return partes.Count == 0 ? "(sem conteúdo)" : string.Join(" — ", partes);
		}

		private static string CapitalizarPrimeiraLetra(string texto)
			=> string.IsNullOrEmpty(texto) ? texto : char.ToUpper(texto[0], new CultureInfo("pt-BR")) + texto[1..];

		[RelayCommand]
		private void FiltrarPendentes() => FiltroSelecionado = "Pendentes";

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