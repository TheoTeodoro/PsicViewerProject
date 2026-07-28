using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.Maui.Audio;
using MauiApp1.Services;

namespace MauiApp1.ViewModels
{
	/// <summary>Uma pergunta no detalhe do histórico — Respondida=false
	/// mostra "Sem resposta"; se tiver áudio de observação, dá pra tocar.</summary>
	public partial class ItemPerguntaHistorico : ObservableObject
	{
		public string Tipo { get; set; } = string.Empty;
		public string Texto { get; set; } = string.Empty;
		public bool Respondida { get; set; }
		public string RespostaExibida { get; set; } = string.Empty;
		public string? AudioObservacao { get; set; }
		public bool TemAudio => !string.IsNullOrEmpty(AudioObservacao);

		/// <summary>Preenchido só quando é uma pergunta de Escala
		/// respondida — mesmo ícone (rosto colorido) usado na hora de
		/// responder, em vez de só texto.</summary>
		public string? IconeEscala { get; set; }
		public bool EhEscala => !string.IsNullOrEmpty(IconeEscala);

		public Color CorFundoCard => Respondida ? Color.FromArgb("#FAFBFC") : Color.FromArgb("#F0F0F0");
		public FontAttributes EstiloResposta => Respondida ? FontAttributes.None : FontAttributes.Italic;

		[ObservableProperty]
		private bool tocandoAudio;

		[ObservableProperty]
		private double progressoAudio;
	}

	/// <summary>Detalhe de UM questionário respondido num DIA específico
	/// do histórico do paciente — só leitura. Reaproveitado também seria
	/// possível pro psicólogo, mas por ora é só do lado do paciente
	/// (revisar o que ele mesmo respondeu, sem poder editar).</summary>
	public partial class DetalheHistoricoQuestionarioViewModel : ObservableObject
	{
		private readonly QuestionarioApiService _questionarios;
		private readonly ArquivoUploadService _upload;
		private readonly IAudioManager _audioManager;

		private IAudioPlayer? _player;
		private IDispatcherTimer? _timer;
		private ItemPerguntaHistorico? _tocando;

		[ObservableProperty]
		private string questionarioTitulo = string.Empty;

		[ObservableProperty]
		private string dataExibida = string.Empty;

		[ObservableProperty]
		private bool carregando;

		[ObservableProperty]
		private string mensagemErro = string.Empty;

		public ObservableCollection<ItemPerguntaHistorico> Perguntas { get; } = new();

		public DetalheHistoricoQuestionarioViewModel(QuestionarioApiService questionarios, ArquivoUploadService upload, IAudioManager audioManager)
		{
			_questionarios = questionarios;
			_upload = upload;
			_audioManager = audioManager;
		}

		public async Task CarregarAsync(Guid questionarioId, Guid pacienteId, string data, string tituloFallback)
		{
			Carregando = true;
			MensagemErro = string.Empty;
			QuestionarioTitulo = tituloFallback;
			try
			{
				var (detalhe, erro) = await _questionarios.ObterHistoricoDetalheAsync(questionarioId, pacienteId, data);
				if (detalhe is null)
				{
					MensagemErro = erro ?? "Não foi possível carregar esse questionário.";
					return;
				}

				QuestionarioTitulo = detalhe.Titulo;
				if (DateOnly.TryParse(detalhe.Data, out var dataConvertida))
					DataExibida = dataConvertida.ToString("dd 'de' MMMM 'de' yyyy", new CultureInfo("pt-BR"));

				Perguntas.Clear();
				foreach (var p in detalhe.Perguntas)
				{
					Perguntas.Add(new ItemPerguntaHistorico
					{
						Tipo = p.Tipo,
						Texto = p.Texto,
						Respondida = p.Respondida,
						RespostaExibida = FormatarResposta(p),
						IconeEscala = p.Respondida && p.Tipo == "Escala" && p.ValorEscala is int nivel ? IconeParaNivel(nivel) : null,
						AudioObservacao = p.AudioObservacao
					});
				}
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

		private static string FormatarResposta(PerguntaHistoricoDetalheDto p)
		{
			if (!p.Respondida) return "Sem resposta";

			var partes = new List<string>();

			if (p.Tipo == "Escala" && p.ValorEscala is int valor)
				partes.Add(DescricaoNivel(valor));
			else if (!string.IsNullOrWhiteSpace(p.RespostaTexto))
				partes.Add(p.RespostaTexto);

			if (!string.IsNullOrWhiteSpace(p.Observacao))
				partes.Add($"Observação: {p.Observacao}");

			return partes.Count == 0 ? "(sem conteúdo além do áudio)" : string.Join(" — ", partes);
		}

		/// <summary>Mesma descrição usada na tela de Dar Feedback do
		/// psicólogo — assume escala de 1 a 5.</summary>
		private static string DescricaoNivel(int valor) => valor switch
		{
			1 => "Muito mal",
			2 => "Mal",
			3 => "Regular",
			4 => "Bem",
			5 => "Muito bem",
			_ => $"Nível {valor}"
		};

		/// <summary>Mesmos ícones (rostos coloridos) usados na hora de
		/// responder uma pergunta de Escala.</summary>
		private static string? IconeParaNivel(int valor) => valor switch
		{
			1 => "escala_muito_mal.svg",
			2 => "escala_mal.svg",
			3 => "escala_regular.svg",
			4 => "escala_bem.svg",
			5 => "escala_muito_bem.svg",
			_ => null
		};

		[RelayCommand]
		private async Task TocarAudioAsync(ItemPerguntaHistorico item)
		{
			if (item is null || string.IsNullOrEmpty(item.AudioObservacao)) return;

			try
			{
				if (_tocando == item && _player is not null)
				{
					if (_player.IsPlaying)
					{
						_player.Pause();
						_timer?.Stop();
						item.TocandoAudio = false;
					}
					else
					{
						_player.Play();
						_timer?.Start();
						item.TocandoAudio = true;
					}
					return;
				}

				PararAudioAtual();

				var urlCompleta = $"{ApiConfig.ServidorBaseUrl}{item.AudioObservacao}";
				var caminhoLocal = await _upload.BaixarAsync(urlCompleta);

				_player = _audioManager.CreatePlayer(File.OpenRead(caminhoLocal));
				_tocando = item;
				_player.PlaybackEnded += (s, e) =>
				{
					MainThread.BeginInvokeOnMainThread(() =>
					{
						_timer?.Stop();
						item.TocandoAudio = false;
						item.ProgressoAudio = 0;
					});
				};

				item.ProgressoAudio = 0;
				_timer = Application.Current!.Dispatcher.CreateTimer();
				_timer.Interval = TimeSpan.FromMilliseconds(200);
				_timer.Tick += (s, e) =>
				{
					if (_player is not null && _player.Duration > 0)
						item.ProgressoAudio = Math.Clamp(_player.CurrentPosition / _player.Duration, 0, 1);
				};
				_timer.Start();

				_player.Play();
				item.TocandoAudio = true;
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível tocar o áudio: " + ex.Message;
			}
		}

		private void PararAudioAtual()
		{
			_timer?.Stop();
			if (_player is not null)
			{
				_player.Stop();
				_player.Dispose();
				_player = null;
			}
			if (_tocando is not null)
			{
				_tocando.TocandoAudio = false;
				_tocando.ProgressoAudio = 0;
			}
			_tocando = null;
		}

		[RelayCommand]
		private async Task VoltarAsync()
		{
			PararAudioAtual();
			await Application.Current!.MainPage!.Navigation.PopAsync();
		}
	}
}