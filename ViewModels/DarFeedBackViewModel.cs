using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.Maui.Audio;
using PsicViewer.Core.Entities;
using MauiApp1.Services;

namespace MauiApp1.ViewModels
{
	/// <summary>Tela em que o psicólogo escreve (ou grava um áudio) um
	/// feedback pra uma resposta específica de questionário — mostra o
	/// contexto (pergunta + resposta do paciente, incluindo o áudio de
	/// observação dele, se tiver) antes de escrever, e o feedback vai pro
	/// chat normal com esse paciente, citando esse contexto.</summary>
	public partial class DarFeedbackViewModel : ObservableObject
	{
		private readonly QuestionarioApiService _questionarios;
		private readonly ChatConnectionService _chat;
		private readonly ArquivoUploadService _upload;
		private readonly IAudioManager _audioManager;
		private readonly SessaoUsuario _sessao;

		private Guid _respostaId;
		private Guid _pacienteId;
		private string? _audioObservacaoCaminho;
		private IAudioRecorder? _gravador;
		private IAudioPlayer? _previaPlayer;
		private IAudioPlayer? _audioObservacaoPlayer;
		private IDispatcherTimer? _timerGravacao;
		private IDispatcherTimer? _timerPrevia;
		private IDispatcherTimer? _timerAudioObservacao;
		private string? _caminhoAudioLocal;

		[ObservableProperty]
		private string pacienteNome = string.Empty;

		[ObservableProperty]
		private string questionarioTitulo = string.Empty;

		[ObservableProperty]
		private string perguntaTexto = string.Empty;

		[ObservableProperty]
		private string respostaExibida = string.Empty;

		[ObservableProperty]
		private string textoFeedback = string.Empty;

		[ObservableProperty]
		private bool carregando;

		[ObservableProperty]
		private string mensagemErro = string.Empty;

		[ObservableProperty]
		private bool gravandoAudio;

		[ObservableProperty]
		private int segundosGravando;

		// Preenchido depois que a gravação para — o áudio fica disponível
		// pra ouvir ou remover, SEM ter sido enviado ainda. Só quando o
		// psicólogo toca em "Enviar Áudio" é que ele é de fato mandado.
		[ObservableProperty]
		private bool audioGravado;

		[ObservableProperty]
		private int duracaoAudioGravado;

		[ObservableProperty]
		private bool tocandoPrevia;

		[ObservableProperty]
		private int segundosRestantesPrevia;

		[ObservableProperty]
		private bool tocandoAudioResposta;

		[ObservableProperty]
		private double progressoAudioResposta;

		[ObservableProperty]
		private bool temAudioResposta;

		public string TempoGravacaoFormatado => TimeSpan.FromSeconds(SegundosGravando).ToString(@"m\:ss");

		public string TempoPreviaFormatado => TimeSpan
			.FromSeconds(TocandoPrevia ? SegundosRestantesPrevia : DuracaoAudioGravado)
			.ToString(@"m\:ss");

		partial void OnSegundosGravandoChanged(int value) => OnPropertyChanged(nameof(TempoGravacaoFormatado));
		partial void OnTocandoPreviaChanged(bool value) => OnPropertyChanged(nameof(TempoPreviaFormatado));
		partial void OnSegundosRestantesPreviaChanged(int value) => OnPropertyChanged(nameof(TempoPreviaFormatado));
		partial void OnDuracaoAudioGravadoChanged(int value) => OnPropertyChanged(nameof(TempoPreviaFormatado));

		public DarFeedbackViewModel(QuestionarioApiService questionarios, ChatConnectionService chat,
			ArquivoUploadService upload, IAudioManager audioManager, SessaoUsuario sessao)
		{
			_questionarios = questionarios;
			_chat = chat;
			_upload = upload;
			_audioManager = audioManager;
			_sessao = sessao;
		}

		public async Task CarregarAsync(Guid respostaId)
		{
			_respostaId = respostaId;
			Carregando = true;
			MensagemErro = string.Empty;
			try
			{
				var detalhe = await _questionarios.ObterDetalheRespostaAsync(respostaId);
				if (detalhe is null)
				{
					MensagemErro = "Não foi possível carregar essa resposta.";
					return;
				}

				_pacienteId = detalhe.PacienteId;
				PacienteNome = detalhe.PacienteNome;
				QuestionarioTitulo = detalhe.QuestionarioTitulo;
				PerguntaTexto = detalhe.PerguntaTexto;
				RespostaExibida = FormatarResposta(detalhe);

				_audioObservacaoCaminho = detalhe.AudioObservacao;
				TemAudioResposta = !string.IsNullOrEmpty(detalhe.AudioObservacao);

				if (!_chat.Conectado)
					await _chat.ConectarAsync(_sessao.UsuarioId);
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

		private static string FormatarResposta(RespostaDetalheDto detalhe)
		{
			var partes = new List<string>();

			if (!string.IsNullOrWhiteSpace(detalhe.RespostaTexto))
				partes.Add(detalhe.RespostaTexto);
			else if (detalhe.ValorEscala is int valor)
				partes.Add(FormatarNivelEscala(valor));

			if (!string.IsNullOrWhiteSpace(detalhe.Observacao))
				partes.Add($"Observação: {detalhe.Observacao}");

			return partes.Count == 0 ? "(sem conteúdo)" : string.Join(" — ", partes);
		}

		/// <summary>Em vez de só "Nível 5", dá o significado por trás do
		/// número — mais revelador pra quem vai dar o feedback. Assume
		/// escala de 1 a 5 (a mais usada nos questionários de humor); um
		/// valor fora disso cai no "Nível N" simples.</summary>
		private static string FormatarNivelEscala(int valor) => valor switch
		{
			1 => "Nível 1 - Muito mal 😞",
			2 => "Nível 2 - Mal 🙁",
			3 => "Nível 3 - Neutro 😐",
			4 => "Nível 4 - Bem 🙂",
			5 => "Nível 5 - Muito bem 😄",
			_ => $"Nível {valor}"
		};

		/// <summary>Manda só o texto — independente de ter (ou não) um
		/// áudio gravado esperando. Pra mandar o áudio, usa EnviarAudio.</summary>
		[RelayCommand]
		private async Task EnviarTextoAsync()
		{
			if (string.IsNullOrWhiteSpace(TextoFeedback))
			{
				MensagemErro = "Escreva o feedback antes de enviar.";
				return;
			}

			Carregando = true;
			MensagemErro = string.Empty;
			try
			{
				await _chat.EnviarFeedbackAsync(_sessao.UsuarioId, _pacienteId, _respostaId,
					TipoConteudoMensagem.Texto, TextoFeedback.Trim());

				PararAudioResposta();
				await Application.Current!.MainPage!.Navigation.PopAsync();
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível enviar o feedback: " + ex.Message;
			}
			finally
			{
				Carregando = false;
			}
		}

		[RelayCommand]
		private async Task IniciarGravacaoAsync()
		{
			try
			{
				var status = await Permissions.RequestAsync<Permissions.Microphone>();
				if (status != PermissionStatus.Granted)
				{
					MensagemErro = "Permissão de microfone negada. Ative em Configurações do celular > Apps > PsicViewer > Permissões.";
					return;
				}

				_gravador = _audioManager.CreateRecorder();
				if (!_gravador.CanRecordAudio)
				{
					MensagemErro = "Sem permissão de microfone.";
					return;
				}

				MensagemErro = string.Empty;
				await _gravador.StartAsync();
				GravandoAudio = true;
				SegundosGravando = 0;

				_timerGravacao = Application.Current!.Dispatcher.CreateTimer();
				_timerGravacao.Interval = TimeSpan.FromSeconds(1);
				_timerGravacao.Tick += (s, e) => SegundosGravando++;
				_timerGravacao.Start();
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível gravar áudio: " + ex.Message;
			}
		}

		/// <summary>SÓ para a gravação — não envia nada. O áudio fica
		/// salvo localmente, disponível pra ouvir de novo ou descartar
		/// antes de decidir enviar (mesmo padrão do chat normal).</summary>
		[RelayCommand]
		private async Task PararGravacaoAsync()
		{
			if (_gravador is null || !GravandoAudio) return;

			GravandoAudio = false;
			_timerGravacao?.Stop();
			var duracaoFinal = SegundosGravando;

			try
			{
				var audioSource = await _gravador.StopAsync();

				var caminhoLocal = Path.Combine(FileSystem.CacheDirectory, $"feedback_{Guid.NewGuid()}.m4a");
				await using (var destino = File.Create(caminhoLocal))
				await using (var origem = audioSource.GetAudioStream())
				{
					await origem.CopyToAsync(destino);
				}

				_caminhoAudioLocal = caminhoLocal;
				DuracaoAudioGravado = duracaoFinal;
				SegundosRestantesPrevia = duracaoFinal;
				AudioGravado = true;
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível salvar o áudio gravado: " + ex.Message;
			}
		}

		[RelayCommand]
		private async Task CancelarGravacaoAsync()
		{
			if (_gravador is null) return;
			GravandoAudio = false;
			_timerGravacao?.Stop();
			try { await _gravador.StopAsync(); } catch { /* ignora, só queremos parar */ }
		}

		/// <summary>Toca (ou pausa) a prévia do áudio já gravado, direto do
		/// arquivo local — ainda não foi enviado, então não precisa
		/// baixar nada, é só tocar o arquivo que já está no celular.</summary>
		[RelayCommand]
		private void TocarPrevia()
		{
			if (_caminhoAudioLocal is null) return;

			try
			{
				if (_previaPlayer is not null)
				{
					if (_previaPlayer.IsPlaying)
					{
						_previaPlayer.Pause();
						_timerPrevia?.Stop();
						TocandoPrevia = false;
					}
					else
					{
						_previaPlayer.Play();
						_timerPrevia?.Start();
						TocandoPrevia = true;
					}
					return;
				}

				_previaPlayer = _audioManager.CreatePlayer(File.OpenRead(_caminhoAudioLocal));
				_previaPlayer.PlaybackEnded += (s, e) =>
				{
					MainThread.BeginInvokeOnMainThread(() =>
					{
						_timerPrevia?.Stop();
						TocandoPrevia = false;
						SegundosRestantesPrevia = DuracaoAudioGravado;
					});
				};

				SegundosRestantesPrevia = DuracaoAudioGravado;

				_timerPrevia = Application.Current!.Dispatcher.CreateTimer();
				_timerPrevia.Interval = TimeSpan.FromSeconds(1);
				_timerPrevia.Tick += (s, e) =>
				{
					if (SegundosRestantesPrevia > 0)
						SegundosRestantesPrevia--;
				};
				_timerPrevia.Start();

				_previaPlayer.Play();
				TocandoPrevia = true;
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível tocar o áudio: " + ex.Message;
			}
		}

		/// <summary>Descarta o áudio gravado (sem enviar) — o psicólogo
		/// pode gravar de novo em seguida.</summary>
		[RelayCommand]
		private void RemoverAudioGravado()
		{
			PararPreviaEliminarPlayer();

			if (_caminhoAudioLocal is not null && File.Exists(_caminhoAudioLocal))
			{
				try { File.Delete(_caminhoAudioLocal); } catch { /* arquivo temporário, sem problema se falhar */ }
			}

			_caminhoAudioLocal = null;
			AudioGravado = false;
			DuracaoAudioGravado = 0;
			SegundosRestantesPrevia = 0;
		}

		/// <summary>Envia de fato o áudio já gravado — se tiver texto
		/// escrito também, ele vai junto como legenda do áudio (mesmo
		/// padrão de anexo+legenda usado no chat normal), em vez de ser
		/// descartado.</summary>
		[RelayCommand]
		private async Task EnviarAudioAsync()
		{
			if (_caminhoAudioLocal is null) return;

			PararPreviaEliminarPlayer();

			Carregando = true;
			MensagemErro = string.Empty;
			try
			{
				var (caminhoServidor, nomeOriginal) = await _upload.EnviarAsync(_caminhoAudioLocal, Path.GetFileName(_caminhoAudioLocal));

				await _chat.EnviarFeedbackAsync(_sessao.UsuarioId, _pacienteId, _respostaId,
					TipoConteudoMensagem.Audio, TextoFeedback.Trim(), caminhoServidor, nomeOriginal, DuracaoAudioGravado);

				PararAudioResposta();
				await Application.Current!.MainPage!.Navigation.PopAsync();
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível enviar o áudio: " + ex.Message;
			}
			finally
			{
				Carregando = false;
			}
		}

		private void PararPreviaEliminarPlayer()
		{
			_timerPrevia?.Stop();
			if (_previaPlayer is not null)
			{
				_previaPlayer.Stop();
				_previaPlayer.Dispose();
				_previaPlayer = null;
			}
			TocandoPrevia = false;
		}

		/// <summary>Toca (ou pausa) o áudio de observação que o PACIENTE
		/// gravou junto com a resposta — baixa do servidor na primeira
		/// vez, igual ao chat, já que aqui (diferente da prévia do
		/// feedback) o arquivo não está no celular.</summary>
		[RelayCommand]
		private async Task TocarAudioRespostaAsync()
		{
			if (string.IsNullOrEmpty(_audioObservacaoCaminho)) return;

			try
			{
				if (_audioObservacaoPlayer is not null)
				{
					if (_audioObservacaoPlayer.IsPlaying)
					{
						_audioObservacaoPlayer.Pause();
						_timerAudioObservacao?.Stop();
						TocandoAudioResposta = false;
					}
					else
					{
						_audioObservacaoPlayer.Play();
						_timerAudioObservacao?.Start();
						TocandoAudioResposta = true;
					}
					return;
				}

				var urlCompleta = $"{ApiConfig.ServidorBaseUrl}{_audioObservacaoCaminho}";
				var caminhoLocal = await _upload.BaixarAsync(urlCompleta);

				_audioObservacaoPlayer = _audioManager.CreatePlayer(File.OpenRead(caminhoLocal));
				_audioObservacaoPlayer.PlaybackEnded += (s, e) =>
				{
					MainThread.BeginInvokeOnMainThread(() =>
					{
						_timerAudioObservacao?.Stop();
						TocandoAudioResposta = false;
						ProgressoAudioResposta = 0;
					});
				};

				ProgressoAudioResposta = 0;
				_timerAudioObservacao = Application.Current!.Dispatcher.CreateTimer();
				_timerAudioObservacao.Interval = TimeSpan.FromMilliseconds(200);
				_timerAudioObservacao.Tick += (s, e) =>
				{
					if (_audioObservacaoPlayer is not null && _audioObservacaoPlayer.Duration > 0)
						ProgressoAudioResposta = Math.Clamp(_audioObservacaoPlayer.CurrentPosition / _audioObservacaoPlayer.Duration, 0, 1);
				};
				_timerAudioObservacao.Start();

				_audioObservacaoPlayer.Play();
				TocandoAudioResposta = true;
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível tocar o áudio da resposta: " + ex.Message;
			}
		}

		private void PararAudioResposta()
		{
			_timerAudioObservacao?.Stop();
			if (_audioObservacaoPlayer is not null)
			{
				_audioObservacaoPlayer.Stop();
				_audioObservacaoPlayer.Dispose();
				_audioObservacaoPlayer = null;
			}
			TocandoAudioResposta = false;
		}

		[RelayCommand]
		private async Task VoltarAsync()
		{
			PararAudioResposta();
			await Application.Current!.MainPage!.Navigation.PopAsync();
		}
	}
}