using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.Maui.Audio;
using MauiApp1.Services;

namespace MauiApp1.ViewModels
{
	public partial class RespostaPerguntaItem : ObservableObject
	{
		private readonly IAudioManager _audioManager;
		private readonly ArquivoUploadService _upload;
		private readonly QuestionarioApiService _questionarios;
		private readonly Guid _questionarioId;
		private readonly Guid _pacienteId;
		private IAudioRecorder? _gravador;

		public Guid PerguntaId { get; set; }
		public string Tipo { get; set; } = "Texto";
		public string TextoPergunta { get; set; } = string.Empty;
		public List<string> Opcoes { get; set; } = new();

		[ObservableProperty]
		private int? escalaSelecionada;

		[ObservableProperty]
		private string? opcaoSelecionada;

		[ObservableProperty]
		private string respostaTextoAtual = string.Empty;

		[ObservableProperty]
		private string observacaoTexto = string.Empty;

		[ObservableProperty]
		private bool gravandoObservacao;

		[ObservableProperty]
		private string? caminhoAudioObservacao;

		[ObservableProperty]
		private bool tocandoAudio;

		[ObservableProperty]
		private double progressoAudio;

		[ObservableProperty]
		private string mensagemErroItem = string.Empty;

		[ObservableProperty]
		private bool enviando;

		[ObservableProperty]
		private bool enviada;

		[ObservableProperty]
		private bool editando;

		private IAudioPlayer? _tocandoPlayer;
		private IDispatcherTimer? _timerReproducao;

		public bool EhEscala => Tipo == "Escala";
		public bool EhTexto => Tipo == "Texto";
		public bool EhMultiplaEscolha => Tipo == "MultiplaEscolha";
		public bool MostrarObservacoes => EhEscala || EhMultiplaEscolha;
		public bool TemAudio => !string.IsNullOrEmpty(CaminhoAudioObservacao);
		public string TextoBotaoTocar => TocandoAudio ? "⏸ Pausar" : "▶ Tocar";

		public bool Trancada => Enviada && !Editando;
		public bool PodeEditar => !Trancada;
		public Color CorFundoCard => Trancada ? Color.FromArgb("#EDEDED") : Color.FromArgb("#FAFBFC");

		public string IconeAudio => Trancada
			? "icone_microfone_barrado.svg"
			: GravandoObservacao ? "icone_parar.svg" : "icone_microfone_branco.svg";

		partial void OnGravandoObservacaoChanged(bool value) => OnPropertyChanged(nameof(IconeAudio));
		partial void OnCaminhoAudioObservacaoChanged(string? value) => OnPropertyChanged(nameof(TemAudio));
		partial void OnTocandoAudioChanged(bool value) => OnPropertyChanged(nameof(TextoBotaoTocar));

		partial void OnEnviadaChanged(bool value) => AtualizarEstadoVisual();
		partial void OnEditandoChanged(bool value) => AtualizarEstadoVisual();

		private void AtualizarEstadoVisual()
		{
			OnPropertyChanged(nameof(Trancada));
			OnPropertyChanged(nameof(PodeEditar));
			OnPropertyChanged(nameof(CorFundoCard));
			OnPropertyChanged(nameof(IconeAudio));
		}

		public RespostaPerguntaItem(IAudioManager audioManager, ArquivoUploadService upload, QuestionarioApiService questionarios,
			Guid questionarioId, Guid pacienteId)
		{
			_audioManager = audioManager;
			_upload = upload;
			_questionarios = questionarios;
			_questionarioId = questionarioId;
			_pacienteId = pacienteId;
		}

		[RelayCommand]
		private void SelecionarEscala(string valor)
		{
			if (int.TryParse(valor, out var v)) EscalaSelecionada = v;
		}

		[RelayCommand]
		private void SelecionarOpcao(string opcao) => OpcaoSelecionada = opcao;

		[RelayCommand]
		private void Editar() => Editando = true;

		[RelayCommand]
		private async Task AlternarGravacaoAsync()
		{
			try
			{
				if (GravandoObservacao)
				{
					GravandoObservacao = false;
					if (_gravador is null) return;

					var audioSource = await _gravador.StopAsync();
					var caminhoLocal = Path.Combine(FileSystem.CacheDirectory, $"obs_{Guid.NewGuid()}.m4a");

					await using (var destino = File.Create(caminhoLocal))
					await using (var origem = audioSource.GetAudioStream())
					{
						await origem.CopyToAsync(destino);
					}

					var (caminhoServidor, _) = await _upload.EnviarAsync(caminhoLocal, Path.GetFileName(caminhoLocal));
					CaminhoAudioObservacao = caminhoServidor;
				}
				else
				{
					var status = await Permissions.RequestAsync<Permissions.Microphone>();
					if (status != PermissionStatus.Granted)
					{
						MensagemErroItem = "Permissão de microfone negada.";
						return;
					}

					_gravador = _audioManager.CreateRecorder();
					await _gravador.StartAsync();
					GravandoObservacao = true;
				}
			}
			catch (Exception ex)
			{
				MensagemErroItem = "Erro no áudio: " + ex.Message;
				GravandoObservacao = false;
			}
		}

		[RelayCommand]
		private void RemoverAudio()
		{
			_timerReproducao?.Stop();
			_tocandoPlayer?.Stop();
			_tocandoPlayer?.Dispose();
			_tocandoPlayer = null;
			TocandoAudio = false;
			ProgressoAudio = 0;
			CaminhoAudioObservacao = null;
		}

		[RelayCommand]
		private async Task TocarAudioObservacaoAsync()
		{
			if (string.IsNullOrEmpty(CaminhoAudioObservacao)) return;

			try
			{
				if (TocandoAudio && _tocandoPlayer is not null)
				{
					_tocandoPlayer.Pause();
					_timerReproducao?.Stop();
					TocandoAudio = false;
					return;
				}

				if (_tocandoPlayer is not null && !TocandoAudio)
				{
					_tocandoPlayer.Play();
					_timerReproducao?.Start();
					TocandoAudio = true;
					return;
				}

				var urlCompleta = $"{ApiConfig.ServidorBaseUrl}{CaminhoAudioObservacao}";
				var caminhoLocal = await _upload.BaixarAsync(urlCompleta);

				_tocandoPlayer = _audioManager.CreatePlayer(File.OpenRead(caminhoLocal));
				_tocandoPlayer.PlaybackEnded += (s, e) =>
				{
					MainThread.BeginInvokeOnMainThread(() =>
					{
						_timerReproducao?.Stop();
						TocandoAudio = false;
						ProgressoAudio = 0;
					});
				};

				ProgressoAudio = 0;
				_timerReproducao = Application.Current!.Dispatcher.CreateTimer();
				_timerReproducao.Interval = TimeSpan.FromMilliseconds(100);
				_timerReproducao.Tick += (s, e) =>
				{
					if (_tocandoPlayer is not null && _tocandoPlayer.Duration > 0)
						ProgressoAudio = Math.Clamp(_tocandoPlayer.CurrentPosition / _tocandoPlayer.Duration, 0, 1);
				};
				_timerReproducao.Start();

				_tocandoPlayer.Play();
				TocandoAudio = true;
			}
			catch (Exception ex)
			{
				MensagemErroItem = "Não foi possível tocar o áudio: " + ex.Message;
			}
		}

		[RelayCommand]
		private async Task EnviarRespostaAsync()
		{
			if (EhEscala && EscalaSelecionada is null)
			{
				MensagemErroItem = "Escolha uma opção antes de enviar.";
				return;
			}

			if (EhMultiplaEscolha && string.IsNullOrEmpty(OpcaoSelecionada))
			{
				MensagemErroItem = "Escolha uma opção antes de enviar.";
				return;
			}

			if (EhTexto && string.IsNullOrWhiteSpace(RespostaTextoAtual))
			{
				MensagemErroItem = "Escreva algo antes de enviar.";
				return;
			}

			MensagemErroItem = string.Empty;
			Enviando = true;
			try
			{
				string? valorPrincipal = EhMultiplaEscolha ? OpcaoSelecionada
					: EhTexto ? RespostaTextoAtual.Trim()
					: null;

				int? valorEscala = EhEscala ? EscalaSelecionada : null;

				string? observacao = MostrarObservacoes && !string.IsNullOrWhiteSpace(ObservacaoTexto)
					? ObservacaoTexto.Trim() : null;

				string? audioObservacao = MostrarObservacoes ? CaminhoAudioObservacao : null;

				var (sucesso, erro) = await _questionarios.ResponderPerguntaAsync(
					_questionarioId, PerguntaId, _pacienteId, valorEscala, valorPrincipal, observacao, audioObservacao);

				if (!sucesso)
				{
					MensagemErroItem = erro ?? "Não foi possível enviar essa resposta.";
					return;
				}

				Enviada = true;
				Editando = false;
			}
			catch (Exception ex)
			{
				MensagemErroItem = "Não foi possível conectar ao servidor: " + ex.Message;
			}
			finally
			{
				Enviando = false;
			}
		}
	}

	public partial class ResponderQuestionarioViewModel : ObservableObject
	{
		private readonly QuestionarioApiService _questionarios;
		private readonly ArquivoUploadService _upload;
		private readonly IAudioManager _audioManager;
		private readonly SessaoUsuario _sessao;

		[ObservableProperty]
		private string tituloQuestionario = string.Empty;

		[ObservableProperty]
		private bool carregando;

		[ObservableProperty]
		private string mensagemErro = string.Empty;

		public ObservableCollection<RespostaPerguntaItem> Perguntas { get; } = new();

		public ResponderQuestionarioViewModel(QuestionarioApiService questionarios, ArquivoUploadService upload, IAudioManager audioManager, SessaoUsuario sessao)
		{
			_questionarios = questionarios;
			_upload = upload;
			_audioManager = audioManager;
			_sessao = sessao;
		}

		public async Task CarregarAsync(Guid questionarioId)
		{
			Carregando = true;
			MensagemErro = string.Empty;
			try
			{
				var questionario = await _questionarios.ObterParaResponderAsync(questionarioId, _sessao.UsuarioId);

				if (questionario is null || questionario.Perguntas.Count == 0)
				{
					MensagemErro = "Não foi possível carregar esse questionário.";
					return;
				}

				TituloQuestionario = questionario.Titulo;

				Perguntas.Clear();
				foreach (var p in questionario.Perguntas)
				{
					var item = new RespostaPerguntaItem(_audioManager, _upload, _questionarios, questionarioId, _sessao.UsuarioId)
					{
						PerguntaId = p.Id,
						Tipo = p.Tipo,
						TextoPergunta = p.Texto
					};

					if (p.Tipo == "MultiplaEscolha" && !string.IsNullOrEmpty(p.Opcoes))
						item.Opcoes = p.Opcoes.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();

					if (p.RespondidaHoje)
					{
						item.EscalaSelecionada = p.ValorEscala;

						if (p.Tipo == "MultiplaEscolha")
							item.OpcaoSelecionada = p.RespostaTexto;
						else if (p.Tipo == "Texto")
							item.RespostaTextoAtual = p.RespostaTexto ?? string.Empty;

						item.ObservacaoTexto = p.Observacao ?? string.Empty;
						item.CaminhoAudioObservacao = p.AudioObservacao;
						item.Enviada = true;
					}

					Perguntas.Add(item);
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

		[RelayCommand]
		private async Task VoltarAsync()
		{
			await Application.Current!.MainPage!.Navigation.PopAsync();
		}

		[RelayCommand]
		private async Task EnviarPerguntasRespondidasAsync()
		{
			await Application.Current!.MainPage!.Navigation.PopAsync();
		}
	}
}