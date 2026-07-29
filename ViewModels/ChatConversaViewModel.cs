using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.Maui.Audio;
using PsicViewer.Core.Entities;
using MauiApp1.Services;
using MauiApp1.Views;

namespace MauiApp1.ViewModels
{
	public partial class MensagemExibicao : ObservableObject
	{
		public Guid Id { get; set; }
		public TipoConteudoMensagem TipoConteudo { get; set; }
		public string Conteudo { get; set; } = string.Empty;
		public string? CaminhoArquivo { get; set; }
		public string? NomeArquivoOriginal { get; set; }
		public int? DuracaoSegundos { get; set; }
		public DateTime EnviadaEm { get; set; }
		public bool EnviadaPorMim { get; set; }

		// Preenchidos só quando essa mensagem é um feedback do psicólogo a
		// uma resposta de questionário.
		public Guid? RespostaId { get; set; }
		public string? CitacaoTextoPergunta { get; set; }
		public string? CitacaoTextoResposta { get; set; }
		public string? CitacaoQuestionarioTitulo { get; set; }
		public bool EhFeedback => RespostaId.HasValue;

		[ObservableProperty]
		private bool excluida;

		[ObservableProperty]
		private bool estaTocando;

		[ObservableProperty]
		private int segundosRestantes;

		public string? UrlCompletaArquivo =>
			string.IsNullOrEmpty(CaminhoArquivo) ? null : $"{ApiConfig.ServidorBaseUrl}{CaminhoArquivo}";

		/// <summary>Enquanto toca, mostra o tempo restante contando pra baixo.
		/// Parado (pausado ou nem começou), mostra a duração total.</summary>
		public string TempoExibidoFormatado => TimeSpan
			.FromSeconds(EstaTocando ? SegundosRestantes : (DuracaoSegundos ?? 0))
			.ToString(@"m\:ss");

		/// <summary>Fração de 0 a 1 de quanto já tocou — usado pra
		/// posicionar o pontinho na barrinha de progresso do áudio.</summary>
		public double ProgressoAudio => (!EstaTocando || DuracaoSegundos is null or 0)
			? 0
			: 1.0 - (SegundosRestantes / (double)DuracaoSegundos.Value);

		public bool EhTexto => TipoConteudo == TipoConteudoMensagem.Texto && !Excluida;
		public bool EhImagem => TipoConteudo == TipoConteudoMensagem.Imagem && !Excluida;
		public bool EhAudio => TipoConteudo == TipoConteudoMensagem.Audio && !Excluida;
		public bool EhDocumento => TipoConteudo == TipoConteudoMensagem.Documento && !Excluida;

		partial void OnExcluidaChanged(bool value)
		{
			OnPropertyChanged(nameof(EhTexto));
			OnPropertyChanged(nameof(EhImagem));
			OnPropertyChanged(nameof(EhAudio));
			OnPropertyChanged(nameof(EhDocumento));
		}

		partial void OnEstaTocandoChanged(bool value)
		{
			OnPropertyChanged(nameof(TempoExibidoFormatado));
			OnPropertyChanged(nameof(ProgressoAudio));
		}

		partial void OnSegundosRestantesChanged(int value)
		{
			OnPropertyChanged(nameof(TempoExibidoFormatado));
			OnPropertyChanged(nameof(ProgressoAudio));
		}
	}

	public partial class ChatConversaViewModel : ObservableObject, IDisposable
	{
		private readonly ChatConnectionService _chat;
		private readonly ArquivoUploadService _upload;
		private readonly IAudioManager _audioManager;
		private readonly SessaoUsuario _sessao;
		private IAudioRecorder? _gravador;
		private IAudioPlayer? _tocandoPlayer;
		private Guid _tocandoMensagemId;
		private IDispatcherTimer? _timerReproducao;
		private Guid _contatoId;

		[ObservableProperty]
		private string nomeContato = string.Empty;

		[ObservableProperty]
		private string textoNovaMensagem = string.Empty;

		[ObservableProperty]
		private bool carregando;

		[ObservableProperty]
		private string mensagemErro = string.Empty;

		[ObservableProperty]
		private bool gravandoAudio;

		[ObservableProperty]
		private bool mostrarMenuAnexo;

		[ObservableProperty]
		private int segundosGravando;

		private IDispatcherTimer? _timerGravacao;

		public string TempoGravacaoFormatado => TimeSpan.FromSeconds(SegundosGravando).ToString(@"m\:ss");

		partial void OnSegundosGravandoChanged(int value) => OnPropertyChanged(nameof(TempoGravacaoFormatado));

		public ObservableCollection<MensagemExibicao> Mensagens { get; } = new();

		public ChatConversaViewModel(
			ChatConnectionService chat,
			ArquivoUploadService upload,
			IAudioManager audioManager,
			SessaoUsuario sessao)
		{
			_chat = chat;
			_upload = upload;
			_audioManager = audioManager;
			_sessao = sessao;
			_chat.MensagemRecebida += OnMensagemRecebida;
			_chat.MensagemExcluida += OnMensagemExcluida;
		}

		public async Task DefinirContatoAsync(Guid contatoId, string nomeContato)
		{
			_contatoId = contatoId;
			NomeContato = nomeContato;
			await CarregarHistoricoAsync();
		}

		private async Task CarregarHistoricoAsync()
		{
			Carregando = true;
			MensagemErro = string.Empty;
			try
			{
				if (!_chat.Conectado)
					await _chat.ConectarAsync(_sessao.UsuarioId);

				var historico = await _chat.ObterHistoricoAsync(_sessao.UsuarioId, _contatoId);

				Mensagens.Clear();
				foreach (var m in historico)
				{
					Mensagens.Add(new MensagemExibicao
					{
						Id = m.MensagemId,
						TipoConteudo = m.TipoConteudo,
						Conteudo = m.Conteudo,
						CaminhoArquivo = m.CaminhoArquivo,
						NomeArquivoOriginal = m.NomeArquivoOriginal,
						DuracaoSegundos = m.DuracaoSegundos,
						SegundosRestantes = m.DuracaoSegundos ?? 0,
						Excluida = m.Excluida,
						EnviadaEm = m.EnviadaEm,
						EnviadaPorMim = m.RemetenteId == _sessao.UsuarioId,
						RespostaId = m.RespostaId,
						CitacaoTextoPergunta = m.CitacaoTextoPergunta,
						CitacaoTextoResposta = m.CitacaoTextoResposta,
						CitacaoQuestionarioTitulo = m.CitacaoQuestionarioTitulo
					});
				}

				// Abrir a conversa marca como lidas as mensagens desse
				// contato — é isso que faz a notificação sumir da lista.
				await _chat.MarcarComoLidasAsync(_contatoId, _sessao.UsuarioId);
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível conectar ao chat: " + ex.Message;
			}
			finally
			{
				Carregando = false;
			}
		}

		[RelayCommand]
		private async Task EnviarAsync()
		{
			if (string.IsNullOrWhiteSpace(TextoNovaMensagem)) return;

			var texto = TextoNovaMensagem.Trim();
			TextoNovaMensagem = string.Empty;

			var novaMensagem = new MensagemExibicao
			{
				TipoConteudo = TipoConteudoMensagem.Texto,
				Conteudo = texto,
				EnviadaEm = DateTime.Now,
				EnviadaPorMim = true
			};
			Mensagens.Add(novaMensagem);

			try
			{
				var id = await _chat.EnviarTextoAsync(_sessao.UsuarioId, _contatoId, texto);
				novaMensagem.Id = id;
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível enviar: " + ex.Message;
			}
		}

		/// <summary>Abre/fecha o menu customizado de anexo (Documento,
		/// Câmera, Galeria), no lugar do menu genérico do sistema.</summary>
		[RelayCommand]
		private void Anexar()
		{
			MostrarMenuAnexo = !MostrarMenuAnexo;
		}

		[RelayCommand]
		private async Task SelecionarDocumentoAsync()
		{
			MostrarMenuAnexo = false;
			try
			{
				var arquivo = await FilePicker.Default.PickAsync();
				if (arquivo is not null)
					await EnviarArquivoAsync(TipoConteudoMensagem.Documento, arquivo.FullPath, arquivo.FileName);
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível anexar o documento: " + ex.Message;
			}
		}

		[RelayCommand]
		private async Task TirarFotoAsync()
		{
			MostrarMenuAnexo = false;
			try
			{
				var statusCamera = await Permissions.RequestAsync<Permissions.Camera>();
				if (statusCamera != PermissionStatus.Granted)
				{
					MensagemErro = "Permissão de câmera negada.";
					return;
				}

				if (!MediaPicker.Default.IsCaptureSupported)
				{
					MensagemErro = "Este dispositivo não suporta captura de foto.";
					return;
				}

				var foto = await MediaPicker.Default.CapturePhotoAsync();
				if (foto is not null)
					await EnviarArquivoAsync(TipoConteudoMensagem.Imagem, foto.FullPath, foto.FileName);
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível tirar a foto: " + ex.Message;
			}
		}

		[RelayCommand]
		private async Task EscolherGaleriaAsync()
		{
			MostrarMenuAnexo = false;
			try
			{
				var fotoGaleria = await MediaPicker.Default.PickPhotoAsync();
				if (fotoGaleria is not null)
					await EnviarArquivoAsync(TipoConteudoMensagem.Imagem, fotoGaleria.FullPath, fotoGaleria.FileName);
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível anexar a imagem: " + ex.Message;
			}
		}

		[RelayCommand]
		private async Task IniciarGravacaoAsync()
		{
			try
			{
				// Declarar no Manifest não basta a partir do Android 6 —
				// precisa pedir em tempo de execução, com o usuário
				// confirmando no popup. Sem isso, o gravador nativo falha
				// com "uninitialized AudioRecord".
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

		[RelayCommand]
		private async Task PararEEnviarAudioAsync()
		{
			if (_gravador is null || !GravandoAudio) return;

			GravandoAudio = false;
			_timerGravacao?.Stop();
			var duracaoFinal = SegundosGravando;

			try
			{
				var audioSource = await _gravador.StopAsync();

				var caminhoLocal = Path.Combine(FileSystem.CacheDirectory, $"audio_{Guid.NewGuid()}.m4a");
				await using (var destino = File.Create(caminhoLocal))
				await using (var origem = audioSource.GetAudioStream())
				{
					await origem.CopyToAsync(destino);
				}

				await EnviarArquivoAsync(TipoConteudoMensagem.Audio, caminhoLocal, Path.GetFileName(caminhoLocal), duracaoFinal);
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível enviar o áudio: " + ex.Message;
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

		private async Task EnviarArquivoAsync(TipoConteudoMensagem tipo, string caminhoLocal, string nomeArquivo, int? duracaoSegundos = null)
		{
			Carregando = true;
			try
			{
				var (caminhoServidor, nomeOriginal) = await _upload.EnviarAsync(caminhoLocal, nomeArquivo);

				var novaMensagem = new MensagemExibicao
				{
					TipoConteudo = tipo,
					CaminhoArquivo = caminhoServidor,
					NomeArquivoOriginal = nomeOriginal,
					DuracaoSegundos = duracaoSegundos,
					SegundosRestantes = duracaoSegundos ?? 0,
					EnviadaEm = DateTime.Now,
					EnviadaPorMim = true
				};
				Mensagens.Add(novaMensagem);

				var id = await _chat.EnviarArquivoAsync(_sessao.UsuarioId, _contatoId, tipo, caminhoServidor, nomeOriginal, duracaoSegundos: duracaoSegundos);
				novaMensagem.Id = id;
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível enviar o arquivo: " + ex.Message;
			}
			finally
			{
				Carregando = false;
			}
		}

		[RelayCommand]
		private async Task VisualizarImagemAsync(MensagemExibicao mensagem)
		{
			if (mensagem?.UrlCompletaArquivo is null) return;
			await Application.Current!.MainPage!.Navigation.PushModalAsync(
				new VisualizarImagemPage(mensagem.UrlCompletaArquivo));
		}

		[RelayCommand]
		private async Task TocarAudioAsync(MensagemExibicao mensagem)
		{
			if (mensagem is null || mensagem.UrlCompletaArquivo is null) return;

			try
			{
				// Tocando esse mesmo áudio agora? Alterna play/pausa, sem
				// perder de onde parou a contagem regressiva.
				if (_tocandoPlayer is not null && _tocandoMensagemId == mensagem.Id)
				{
					if (_tocandoPlayer.IsPlaying)
					{
						_tocandoPlayer.Pause();
						_timerReproducao?.Stop();
						mensagem.EstaTocando = false;
					}
					else
					{
						_tocandoPlayer.Play();
						_timerReproducao?.Start();
						mensagem.EstaTocando = true;
					}
					return;
				}

				// Trocou de áudio — para o anterior e devolve o tempo dele
				// pro total (não fica "travado" numa contagem no meio).
				if (_tocandoPlayer is not null)
				{
					_timerReproducao?.Stop();
					_tocandoPlayer.Stop();
					_tocandoPlayer.Dispose();
					var anterior = Mensagens.FirstOrDefault(m => m.Id == _tocandoMensagemId);
					if (anterior is not null)
					{
						anterior.EstaTocando = false;
						anterior.SegundosRestantes = anterior.DuracaoSegundos ?? 0;
					}
				}

				var caminhoLocal = await _upload.BaixarAsync(mensagem.UrlCompletaArquivo);

				_tocandoPlayer = _audioManager.CreatePlayer(File.OpenRead(caminhoLocal));
				_tocandoPlayer.PlaybackEnded += (s, e) =>
				{
					MainThread.BeginInvokeOnMainThread(() =>
					{
						_timerReproducao?.Stop();
						mensagem.EstaTocando = false;
						mensagem.SegundosRestantes = mensagem.DuracaoSegundos ?? 0;
					});
				};

				_tocandoMensagemId = mensagem.Id;
				mensagem.SegundosRestantes = mensagem.DuracaoSegundos ?? 0;

				_timerReproducao = Application.Current!.Dispatcher.CreateTimer();
				_timerReproducao.Interval = TimeSpan.FromSeconds(1);
				_timerReproducao.Tick += (s, e) =>
				{
					if (mensagem.SegundosRestantes > 0)
						mensagem.SegundosRestantes--;
				};
				_timerReproducao.Start();

				_tocandoPlayer.Play();
				mensagem.EstaTocando = true;
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível tocar o áudio: " + ex.Message;
			}
		}

		[RelayCommand]
		private async Task AbrirDocumentoAsync(MensagemExibicao mensagem)
		{
			if (mensagem?.UrlCompletaArquivo is null) return;

			try
			{
				// Abre com o app padrão do celular pra esse tipo de arquivo
				// (visualizador de PDF, navegador fazendo download, etc).
				await Launcher.Default.OpenAsync(mensagem.UrlCompletaArquivo);
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível abrir o arquivo: " + ex.Message;
			}
		}

		[RelayCommand]
		private async Task ExcluirMensagemAsync(MensagemExibicao mensagem)
		{
			if (mensagem is null || !mensagem.EnviadaPorMim || mensagem.Id == Guid.Empty) return;

			try
			{
				await _chat.ExcluirMensagemAsync(mensagem.Id, _sessao.UsuarioId);
				MarcarComoExcluidaNaTela(mensagem.Id);
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível apagar: " + ex.Message;
			}
		}

		private void MarcarComoExcluidaNaTela(Guid mensagemId)
		{
			var msg = Mensagens.FirstOrDefault(m => m.Id == mensagemId);
			if (msg is null) return;

			// Agora que MensagemExibicao é observável, só setar já atualiza
			// a tela sozinho — não precisa mais do truque de remover e
			// reinserir o item na lista.
			msg.Excluida = true;
			msg.Conteudo = string.Empty;
			msg.CaminhoArquivo = null;
		}

		private void OnMensagemRecebida(object? sender, MensagemRecebidaEventArgs e)
		{
			if (e.RemetenteId != _contatoId) return;

			MainThread.BeginInvokeOnMainThread(() =>
			{
				Mensagens.Add(new MensagemExibicao
				{
					Id = e.MensagemId,
					TipoConteudo = e.TipoConteudo,
					Conteudo = e.Conteudo,
					CaminhoArquivo = e.CaminhoArquivo,
					NomeArquivoOriginal = e.NomeArquivoOriginal,
					DuracaoSegundos = e.DuracaoSegundos,
					SegundosRestantes = e.DuracaoSegundos ?? 0,
					Excluida = e.Excluida,
					EnviadaEm = e.EnviadaEm,
					EnviadaPorMim = false,
					RespostaId = e.RespostaId,
					CitacaoTextoPergunta = e.CitacaoTextoPergunta,
					CitacaoTextoResposta = e.CitacaoTextoResposta,
					CitacaoQuestionarioTitulo = e.CitacaoQuestionarioTitulo
				});
			});
		}

		private void OnMensagemExcluida(object? sender, MensagemExcluidaEventArgs e)
		{
			MainThread.BeginInvokeOnMainThread(() => MarcarComoExcluidaNaTela(e.MensagemId));
		}

		/// <summary>Sem isso, cada vez que a tela de conversa abre, um novo
		/// ViewModel se inscreve nos eventos do ChatConnectionService
		/// (que é Singleton) e nunca se desinscreve — acumulando handlers
		/// de telas antigas. Chamado pelo code-behind no OnDisappearing.</summary>
		public void Dispose()
		{
			_chat.MensagemRecebida -= OnMensagemRecebida;
			_chat.MensagemExcluida -= OnMensagemExcluida;
			_timerReproducao?.Stop();
			_tocandoPlayer?.Stop();
			_tocandoPlayer?.Dispose();
		}
	}
}