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
	/// <summary>Histórico de respostas de UM paciente específico, visto
	/// pelo psicólogo — só leitura, e só dos questionários que esse
	/// psicólogo mesmo vinculou a ele (o servidor já garante isso).
	/// Reaproveita ItemHistoricoQuestionario/GrupoHistoricoQuestionario
	/// já definidos em QuestionariosPacienteViewModel.cs — mesmo
	/// agrupamento por questionário+dia, mesmo comportamento clicável,
	/// usado no histórico do próprio paciente.</summary>
	public partial class HistoricoPacienteViewModel : ObservableObject
	{
		private readonly QuestionarioApiService _questionarios;
		private readonly SessaoUsuario _sessao;
		private readonly IServiceProvider _serviceProvider;

		private Guid _pacienteId;

		[ObservableProperty]
		private string pacienteNome = string.Empty;

		[ObservableProperty]
		private bool carregando;

		[ObservableProperty]
		private string mensagemErro = string.Empty;

		public ObservableCollection<GrupoHistoricoQuestionario> Grupos { get; } = new();

		public HistoricoPacienteViewModel(QuestionarioApiService questionarios, SessaoUsuario sessao, IServiceProvider serviceProvider)
		{
			_questionarios = questionarios;
			_sessao = sessao;
			_serviceProvider = serviceProvider;
		}

		public async Task CarregarAsync(Guid pacienteId, string pacienteNome)
		{
			_pacienteId = pacienteId;
			PacienteNome = pacienteNome;
			Carregando = true;
			MensagemErro = string.Empty;
			try
			{
				var historico = await _questionarios.ListarHistoricoPacientePorPsicologoAsync(_sessao.UsuarioId, pacienteId);

				// Agrupa por questionário+dia (uma "submissão"), igual ao
				// histórico do próprio paciente — clicável, abre o
				// detalhe (pergunta a pergunta) daquele dia específico.
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

				Grupos.Clear();
				foreach (var grupo in agrupados)
					Grupos.Add(grupo);
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível carregar o histórico: " + ex.Message;
			}
			finally
			{
				Carregando = false;
			}
		}

		private static string CapitalizarPrimeiraLetra(string texto)
			=> string.IsNullOrEmpty(texto) ? texto : char.ToUpper(texto[0], new CultureInfo("pt-BR")) + texto[1..];

		/// <summary>Abre o detalhe (pergunta a pergunta) de um questionário
		/// respondido naquele dia — mesma tela usada pelo paciente pra
		/// rever as próprias respostas, só que aqui é o psicólogo vendo
		/// as do paciente. Só leitura dos dois lados.</summary>
		[RelayCommand]
		private async Task AbrirSubmissaoAsync(ItemHistoricoQuestionario item)
		{
			if (item is null) return;

			var page = _serviceProvider.GetRequiredService<DetalheHistoricoQuestionarioPage>();
			if (page.BindingContext is DetalheHistoricoQuestionarioViewModel vm)
				await vm.CarregarAsync(item.QuestionarioId, _pacienteId, item.DataParaApi, item.QuestionarioTitulo);

			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}

		[RelayCommand]
		private async Task VoltarAsync() => await Application.Current!.MainPage!.Navigation.PopAsync();
	}
}