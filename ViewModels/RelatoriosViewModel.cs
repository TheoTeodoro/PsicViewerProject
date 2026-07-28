using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Graphics;
using Plugin.Maui.Audio;
using MauiApp1.Services;

namespace MauiApp1.ViewModels
{
	/// <summary>Um paciente selecionável no Picker de Relatórios.</summary>
	public class ItemPacienteRelatorio
	{
		public Guid Id { get; set; }
		public string Nome { get; set; } = string.Empty;
		public override string ToString() => Nome;
	}

	/// <summary>Um questionário selecionável (desse psicólogo).</summary>
	public class ItemQuestionarioRelatorio
	{
		public Guid Id { get; set; }
		public string Titulo { get; set; } = string.Empty;
		public override string ToString() => Titulo;
	}

	/// <summary>Uma pergunta na lista de checkboxes — de Escala (ganha
	/// uma Cor fixa, usada na linha do gráfico) ou de Múltipla Escolha
	/// (pra destacar, sem cor própria — usa o esquema fixo Sim/Não/
	/// não respondeu).</summary>
	public partial class ItemPerguntaSelecionavel : ObservableObject
	{
		public Guid Id { get; set; }
		public string Texto { get; set; } = string.Empty;
		public Color Cor { get; set; } = Colors.Gray;

		[ObservableProperty]
		private bool selecionada;
	}

	/// <summary>RF22 — Visualizar Relatórios de Humor. Fluxo: paciente →
	/// questionário → marca uma ou mais perguntas de Escala (cada uma
	/// vira uma linha colorida) → opcionalmente marca uma ou mais
	/// perguntas de Múltipla Escolha pra destacar (fileiras de
	/// marcadores Sim/Não/não respondeu abaixo do gráfico) → período.
	/// Tocar num ponto da linha mostra um balão com a observação (e
	/// áudio, se tiver) daquele dia. Só mostra dados dos questionários
	/// que ESSE psicólogo mesmo criou/vinculou (RF22.4) — o servidor
	/// garante isso.</summary>
	public partial class RelatoriosViewModel : ObservableObject
	{
		private readonly QuestionarioApiService _questionarios;
		private readonly VinculoApiService _vinculo;
		private readonly ArquivoUploadService _upload;
		private readonly IAudioManager _audioManager;
		private readonly SessaoUsuario _sessao;

		private static readonly Color[] PaletaCores =
		{
			Color.FromArgb("#004AAD"),
			Color.FromArgb("#1F9D55"),
			Color.FromArgb("#E8A33D"),
			Color.FromArgb("#8E44AD"),
			Color.FromArgb("#D9534F"),
			Color.FromArgb("#17A2B8"),
		};

		public ObservableCollection<ItemPacienteRelatorio> Pacientes { get; } = new();
		public ObservableCollection<ItemQuestionarioRelatorio> Questionarios { get; } = new();
		public ObservableCollection<ItemPerguntaSelecionavel> PerguntasEscala { get; } = new();
		public ObservableCollection<ItemPerguntaSelecionavel> PerguntasDestaque { get; } = new();

		[ObservableProperty]
		private ItemPacienteRelatorio? pacienteSelecionado;

		[ObservableProperty]
		private ItemQuestionarioRelatorio? questionarioSelecionado;

		[ObservableProperty]
		private bool temPerguntaEscala = true;

		[ObservableProperty]
		private bool temDestaqueAtivo;

		[ObservableProperty]
		private string periodoSelecionado = "Mensal";

		[ObservableProperty]
		private DateTime dataInicioPersonalizada = DateTime.Today.AddMonths(-1);

		[ObservableProperty]
		private DateTime dataFimPersonalizada = DateTime.Today;

		public bool MostrarDatasPersonalizadas => PeriodoSelecionado == "Personalizado";

		public GraficoHumorDrawable Grafico { get; } = new();

		[ObservableProperty]
		private bool temDados;

		[ObservableProperty]
		private bool carregando;

		[ObservableProperty]
		private string mensagemErro = string.Empty;

		// Balão de fala (mostrado ao tocar num ponto do gráfico)
		[ObservableProperty]
		private bool mostrarBalao;

		[ObservableProperty]
		private string textoBalao = string.Empty;

		[ObservableProperty]
		private string? audioBalaoCaminho;

		[ObservableProperty]
		private bool temAudioBalao;

		[ObservableProperty]
		private double balaoX;

		[ObservableProperty]
		private double balaoY;

		[ObservableProperty]
		private bool tocandoAudioBalao;

		[ObservableProperty]
		private double progressoAudioBalao;

		private PontoGrafico? _pontoBalaoAtual;
		private IAudioPlayer? _audioBalaoPlayer;
		private IDispatcherTimer? _timerAudioBalao;

		partial void OnPeriodoSelecionadoChanged(string value)
		{
			OnPropertyChanged(nameof(MostrarDatasPersonalizadas));
		}

		partial void OnPacienteSelecionadoChanged(ItemPacienteRelatorio? value) => _ = CarregarQuestionariosAsync();
		partial void OnQuestionarioSelecionadoChanged(ItemQuestionarioRelatorio? value) => _ = CarregarPerguntasAsync();

		public RelatoriosViewModel(QuestionarioApiService questionarios, VinculoApiService vinculo,
			ArquivoUploadService upload, IAudioManager audioManager, SessaoUsuario sessao)
		{
			_questionarios = questionarios;
			_vinculo = vinculo;
			_upload = upload;
			_audioManager = audioManager;
			_sessao = sessao;
		}

		[RelayCommand]
		private async Task CarregarAsync()
		{
			Carregando = true;
			MensagemErro = string.Empty;
			try
			{
				var vinculos = await _vinculo.ListarPorPsicologoAsync(_sessao.UsuarioId);

				Pacientes.Clear();
				foreach (var v in vinculos.Where(v => v.Status == "Aceito"))
					Pacientes.Add(new ItemPacienteRelatorio { Id = v.ContatoId, Nome = v.ContatoNome });

				if (Pacientes.Count > 0)
					PacienteSelecionado = Pacientes[0];
				else
					Carregando = false;
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível carregar seus pacientes: " + ex.Message;
				Carregando = false;
			}
		}

		private async Task CarregarQuestionariosAsync()
		{
			FecharBalao();
			Questionarios.Clear();
			QuestionarioSelecionado = null;

			try
			{
				var lista = await _questionarios.ListarPorPsicologoAsync(_sessao.UsuarioId);
				foreach (var q in lista)
					Questionarios.Add(new ItemQuestionarioRelatorio { Id = q.Id, Titulo = q.Titulo });

				if (Questionarios.Count > 0)
					QuestionarioSelecionado = Questionarios[0];
				else
					Carregando = false;
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível carregar os questionários: " + ex.Message;
				Carregando = false;
			}
		}

		private async Task CarregarPerguntasAsync()
		{
			FecharBalao();
			PerguntasEscala.Clear();
			PerguntasDestaque.Clear();

			if (QuestionarioSelecionado is null)
			{
				Carregando = false;
				return;
			}

			try
			{
				var detalhe = await _questionarios.ObterParaEditarAsync(QuestionarioSelecionado.Id);
				if (detalhe is null)
				{
					Carregando = false;
					return;
				}

				var corIndice = 0;
				foreach (var p in detalhe.Perguntas.Where(p => p.Tipo == "Escala"))
				{
					var item = new ItemPerguntaSelecionavel { Id = p.Id, Texto = p.Texto, Cor = PaletaCores[corIndice % PaletaCores.Length] };
					PerguntasEscala.Add(item);
					corIndice++;
				}

				foreach (var p in detalhe.Perguntas.Where(p => p.Tipo == "MultiplaEscolha"))
				{
					var item = new ItemPerguntaSelecionavel { Id = p.Id, Texto = p.Texto };
					PerguntasDestaque.Add(item);
				}

				TemPerguntaEscala = PerguntasEscala.Count > 0;

				if (TemPerguntaEscala)
				{
					PerguntasEscala[0].Selecionada = true;
					await CarregarGraficoAsync(); // só o carregamento inicial é automático
				}
				else
				{
					Carregando = false;
				}
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível carregar as perguntas: " + ex.Message;
				Carregando = false;
			}
		}

		[RelayCommand]
		private async Task CarregarGraficoAsync()
		{
			if (PacienteSelecionado is null) return;

			FecharBalao();
			Carregando = true;
			MensagemErro = string.Empty;
			try
			{
				var (inicio, fim) = ObterPeriodo();

				var novasSeries = new List<SerieEscala>();
				foreach (var pergunta in PerguntasEscala.Where(p => p.Selecionada))
				{
					var serieDados = await _questionarios.ObterSeriePerguntaAsync(_sessao.UsuarioId, PacienteSelecionado.Id, pergunta.Id, inicio, fim);

					novasSeries.Add(new SerieEscala
					{
						Nome = pergunta.Texto,
						Cor = pergunta.Cor,
						Pontos = serieDados
							.Where(p => p.ValorEscala.HasValue)
							.Select(p => new PontoGrafico
							{
								Data = DateTime.Parse(p.Data),
								Valor = p.ValorEscala!.Value,
								Observacao = p.Observacao,
								AudioObservacao = p.AudioObservacao
							}).ToList()
					});
				}

				Grafico.Series = novasSeries;
				TemDados = novasSeries.Any(s => s.Pontos.Count > 0);

				var novosDestaques = new List<SerieDestaque>();
				foreach (var pergunta in PerguntasDestaque.Where(p => p.Selecionada))
				{
					var serieDados = await _questionarios.ObterSeriePerguntaAsync(_sessao.UsuarioId, PacienteSelecionado.Id, pergunta.Id, inicio, fim);

					novosDestaques.Add(new SerieDestaque
					{
						Nome = pergunta.Texto,
						RespostasPorDia = serieDados
							.Where(p => !string.IsNullOrEmpty(p.RespostaTexto))
							.ToDictionary(p => DateTime.Parse(p.Data).Date, p => p.RespostaTexto!)
					});
				}

				Grafico.Destaques = novosDestaques;
				TemDestaqueAtivo = novosDestaques.Count > 0;

				OnPropertyChanged(nameof(Grafico));
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível carregar o relatório: " + ex.Message;
				TemDados = false;
			}
			finally
			{
				Carregando = false;
			}
		}

		private (DateTime?, DateTime?) ObterPeriodo()
		{
			if (PeriodoSelecionado == "Semanal") return (DateTime.Today.AddDays(-6), DateTime.Today);
			if (PeriodoSelecionado == "Mensal") return (DateTime.Today.AddDays(-29), DateTime.Today);
			if (PeriodoSelecionado == "Personalizado") return (DataInicioPersonalizada, DataFimPersonalizada);
			return (null, null);
		}

		[RelayCommand]
		private void SetPeriodo(string periodo) => PeriodoSelecionado = periodo;

		/// <summary>Chamado pelo code-behind quando o psicólogo toca no
		/// GraphicsView (x,y já nas coordenadas do próprio gráfico). Um
		/// segundo toque no MESMO ponto fecha o balão; tocar em outro
		/// ponto troca pra ele; tocar fora de qualquer ponto fecha.</summary>
		public void AoTocarNoGrafico(float x, float y)
		{
			var achado = Grafico.Hit(new PointF(x, y));

			if (achado is null)
			{
				FecharBalao();
				return;
			}

			var ponto = achado.Value.Ponto;

			if (MostrarBalao && ReferenceEquals(_pontoBalaoAtual, ponto))
			{
				FecharBalao();
				return;
			}

			PararAudioBalao();

			_pontoBalaoAtual = ponto;
			TextoBalao = string.IsNullOrWhiteSpace(ponto.Observacao)
				? "Sem observação nesse dia."
				: TruncarTexto(ponto.Observacao, 160);
			AudioBalaoCaminho = ponto.AudioObservacao;
			TemAudioBalao = !string.IsNullOrEmpty(ponto.AudioObservacao);

			// Posição aproximada perto do ponto tocado, sem deixar vazar
			// pra fora da área visível do cartão do gráfico.
			BalaoX = Math.Clamp(achado.Value.Centro.X - 90, 4, 300);
			BalaoY = Math.Max(achado.Value.Centro.Y - 90, 4);

			MostrarBalao = true;
		}

		private static string TruncarTexto(string texto, int max)
			=> texto.Length <= max ? texto : texto.Substring(0, max).TrimEnd() + "...";

		[RelayCommand]
		private void FecharBalao()
		{
			PararAudioBalao();
			MostrarBalao = false;
			_pontoBalaoAtual = null;
		}

		[RelayCommand]
		private async Task TocarAudioBalaoAsync()
		{
			if (string.IsNullOrEmpty(AudioBalaoCaminho)) return;

			try
			{
				if (_audioBalaoPlayer is not null)
				{
					if (_audioBalaoPlayer.IsPlaying)
					{
						_audioBalaoPlayer.Pause();
						_timerAudioBalao?.Stop();
						TocandoAudioBalao = false;
					}
					else
					{
						_audioBalaoPlayer.Play();
						_timerAudioBalao?.Start();
						TocandoAudioBalao = true;
					}
					return;
				}

				var urlCompleta = $"{ApiConfig.ServidorBaseUrl}{AudioBalaoCaminho}";
				var caminhoLocal = await _upload.BaixarAsync(urlCompleta);

				_audioBalaoPlayer = _audioManager.CreatePlayer(File.OpenRead(caminhoLocal));
				_audioBalaoPlayer.PlaybackEnded += (s, e) =>
				{
					MainThread.BeginInvokeOnMainThread(() =>
					{
						_timerAudioBalao?.Stop();
						TocandoAudioBalao = false;
						ProgressoAudioBalao = 0;
					});
				};

				ProgressoAudioBalao = 0;
				_timerAudioBalao = Application.Current!.Dispatcher.CreateTimer();
				_timerAudioBalao.Interval = TimeSpan.FromMilliseconds(200);
				_timerAudioBalao.Tick += (s, e) =>
				{
					if (_audioBalaoPlayer is not null && _audioBalaoPlayer.Duration > 0)
						ProgressoAudioBalao = Math.Clamp(_audioBalaoPlayer.CurrentPosition / _audioBalaoPlayer.Duration, 0, 1);
				};
				_timerAudioBalao.Start();

				_audioBalaoPlayer.Play();
				TocandoAudioBalao = true;
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível tocar o áudio: " + ex.Message;
			}
		}

		private void PararAudioBalao()
		{
			_timerAudioBalao?.Stop();
			if (_audioBalaoPlayer is not null)
			{
				_audioBalaoPlayer.Stop();
				_audioBalaoPlayer.Dispose();
				_audioBalaoPlayer = null;
			}
			TocandoAudioBalao = false;
			ProgressoAudioBalao = 0;
		}

		[RelayCommand]
		private async Task VoltarAsync()
		{
			PararAudioBalao();
			await Application.Current!.MainPage!.Navigation.PopAsync();
		}
	}
}