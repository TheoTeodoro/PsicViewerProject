using System;
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
	/// <summary>Um questionário pronto pra exibir na lista — acrescenta
	/// campos calculados (data formatada, cores do selo de status) em
	/// cima do QuestionarioDto puro que vem da API.</summary>
	public class ItemQuestionario
	{
		public Guid Id { get; set; }
		public string Titulo { get; set; } = string.Empty;
		public string Status { get; set; } = string.Empty;
		public DateTime CriadoEm { get; set; }
		public int QuantidadePerguntas { get; set; }

		public string DataExibida => CriadoEm.ToString("ddd, dd MMM", new CultureInfo("pt-BR"));

		public string TextoPerguntas => QuantidadePerguntas == 1
			? "1 pergunta"
			: $"{QuantidadePerguntas} perguntas";

		public Color CorFundoStatus => Status == "Ativo" ? Color.FromArgb("#D7F5E3") : Color.FromArgb("#FBDADA");
		public Color CorTextoStatus => Status == "Ativo" ? Color.FromArgb("#1F9D55") : Color.FromArgb("#D9534F");
	}

	/// <summary>Um grupo de questionários no mesmo mês — pro cabeçalho tipo
	/// "Fevereiro, 2026" que aparece antes de cada bloco na lista.</summary>
	public class GrupoQuestionarios : List<ItemQuestionario>
	{
		public string NomeGrupo { get; }

		public GrupoQuestionarios(string nomeGrupo, List<ItemQuestionario> itens) : base(itens)
		{
			NomeGrupo = nomeGrupo;
		}
	}

	public partial class QuestionariosViewModel : ObservableObject
	{
		private readonly QuestionarioApiService _questionarios;
		private readonly SessaoUsuario _sessao;
		private readonly IServiceProvider _serviceProvider;

		private List<QuestionarioDto> _todosCarregados = new();

		public ObservableCollection<GrupoQuestionarios> Grupos { get; } = new();

		[ObservableProperty]
		private bool carregando;

		[ObservableProperty]
		private string textoBusca = string.Empty;

		[ObservableProperty]
		private string filtroSelecionado = "Todos";

		// Cores dos 3 chips de filtro, calculadas aqui em vez de um
		// converter — mais simples pra esse caso (só 3 opções fixas).
		public Color CorFundoTodos => FiltroSelecionado == "Todos" ? Color.FromArgb("#004AAD") : Colors.White;
		public Color CorFundoAtivos => FiltroSelecionado == "Ativos" ? Color.FromArgb("#004AAD") : Colors.White;
		public Color CorFundoInativos => FiltroSelecionado == "Inativos" ? Color.FromArgb("#004AAD") : Colors.White;
		public Color CorTextoTodos => FiltroSelecionado == "Todos"
			? Colors.White
			: (Application.Current?.Resources.TryGetValue("AzulEscuro", out var cor) == true ? (Color)cor : Colors.Black);

		partial void OnFiltroSelecionadoChanged(string value)
		{
			OnPropertyChanged(nameof(CorFundoTodos));
			OnPropertyChanged(nameof(CorFundoAtivos));
			OnPropertyChanged(nameof(CorFundoInativos));
			OnPropertyChanged(nameof(CorTextoTodos));
		}

		public string FotoExibida => string.IsNullOrEmpty(_sessao.FotoUrl)
			? "avatar_placeholder.jpg"
			: $"{ApiConfig.ServidorBaseUrl}{_sessao.FotoUrl}";

		partial void OnTextoBuscaChanged(string value) => AplicarFiltro();

		public QuestionariosViewModel(QuestionarioApiService questionarios, SessaoUsuario sessao, IServiceProvider serviceProvider)
		{
			_questionarios = questionarios;
			_sessao = sessao;
			_serviceProvider = serviceProvider;
		}

		[RelayCommand]
		private async Task CarregarAsync()
		{
			Carregando = true;
			try
			{
				_todosCarregados = await _questionarios.ListarPorPsicologoAsync(_sessao.UsuarioId);
				AplicarFiltro();
			}
			finally
			{
				Carregando = false;
			}
		}

		[RelayCommand]
		private void FiltrarTodos() => DefinirFiltro("Todos");

		[RelayCommand]
		private void FiltrarAtivos() => DefinirFiltro("Ativos");

		// "Inativo" = questionário sem NENHUM paciente vinculado no
		// momento (o servidor já manda o Status calculado assim, em vez
		// do status manual de arquivar que existia antes).
		[RelayCommand]
		private void FiltrarInativos() => DefinirFiltro("Inativos");

		private void DefinirFiltro(string filtro)
		{
			FiltroSelecionado = filtro;
			AplicarFiltro();
		}

		private void AplicarFiltro()
		{
			var termo = TextoBusca?.Trim() ?? string.Empty;

			var filtrados = _todosCarregados.Where(q =>
				(FiltroSelecionado == "Todos"
					|| (FiltroSelecionado == "Ativos" && q.Status == "Ativo")
					|| (FiltroSelecionado == "Inativos" && q.Status == "Inativo"))
				&& (string.IsNullOrEmpty(termo) || q.Titulo.Contains(termo, StringComparison.OrdinalIgnoreCase)));

			var itens = filtrados.Select(q => new ItemQuestionario
			{
				Id = q.Id,
				Titulo = q.Titulo,
				Status = q.Status,
				CriadoEm = q.CriadoEm,
				QuantidadePerguntas = q.QuantidadePerguntas
			});

			var agrupados = itens
				.GroupBy(i => new DateTime(i.CriadoEm.Year, i.CriadoEm.Month, 1))
				.OrderByDescending(g => g.Key)
				.Select(g => new GrupoQuestionarios(
					CapitalizarPrimeiraLetra(g.Key.ToString("MMMM, yyyy", new CultureInfo("pt-BR"))),
					g.OrderByDescending(i => i.CriadoEm).ToList()));

			Grupos.Clear();
			foreach (var grupo in agrupados)
				Grupos.Add(grupo);
		}

		private static string CapitalizarPrimeiraLetra(string texto)
			=> string.IsNullOrEmpty(texto) ? texto : char.ToUpper(texto[0], new CultureInfo("pt-BR")) + texto[1..];

		[RelayCommand]
		private async Task AbrirCriarAsync()
		{
			var page = _serviceProvider.GetRequiredService<CriarQuestionarioPage>();
			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}

		[RelayCommand]
		private async Task AbrirQuestionarioAsync(ItemQuestionario item)
		{
			if (item is null) return;

			var page = _serviceProvider.GetRequiredService<EditarQuestionarioPage>();
			if (page.BindingContext is EditarQuestionarioViewModel vm)
				await vm.CarregarAsync(item.Id);

			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		}

		/// <summary>Apaga o questionário de vez (com confirmação) —
		/// perguntas, respostas e vínculos com pacientes vão junto, não
		/// tem volta. Diferente de desativar/inativar, que é reversível.</summary>
		[RelayCommand]
		private async Task RemoverQuestionarioAsync(ItemQuestionario item)
		{
			if (item is null) return;

			var confirmar = await Application.Current!.MainPage!.DisplayAlert(
				"Apagar questionário",
				$"Tem certeza que deseja apagar \"{item.Titulo}\"? Isso remove todas as perguntas e respostas registradas dele. Essa ação não pode ser desfeita.",
				"Apagar", "Cancelar");

			if (!confirmar) return;

			var (sucesso, erro) = await _questionarios.ExcluirAsync(item.Id);
			if (!sucesso)
			{
				await Application.Current!.MainPage!.DisplayAlert("Erro", erro ?? "Não foi possível apagar o questionário.", "OK");
				return;
			}

			_todosCarregados.RemoveAll(q => q.Id == item.Id);
			AplicarFiltro();
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