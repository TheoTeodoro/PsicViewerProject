using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using PsicViewer.Core.Entities;

namespace MauiApp1.Services
{
	public class MensagemRecebidaEventArgs : EventArgs
	{
		public Guid MensagemId { get; set; }
		public Guid RemetenteId { get; set; }
		public Guid DestinatarioId { get; set; }
		public TipoConteudoMensagem TipoConteudo { get; set; }
		public string Conteudo { get; set; } = string.Empty;
		public string? CaminhoArquivo { get; set; }
		public string? NomeArquivoOriginal { get; set; }
		public int? DuracaoSegundos { get; set; }
		public bool Excluida { get; set; }
		public DateTime EnviadaEm { get; set; }

		// Preenchidos só quando essa mensagem é um FEEDBACK do psicólogo a
		// uma resposta de questionário — RespostaId aponta pra resposta
		// original, e os dois textos de citação são o "congelamento" da
		// pergunta e da resposta no momento do feedback (tipo citar
		// mensagem no WhatsApp).
		public Guid? RespostaId { get; set; }
		public string? CitacaoTextoPergunta { get; set; }
		public string? CitacaoTextoResposta { get; set; }
	}

	public class MensagemExcluidaEventArgs : EventArgs
	{
		public Guid MensagemId { get; set; }
	}

	public class ChatConnectionService
	{
		private HubConnection? _connection;

		public event EventHandler<MensagemRecebidaEventArgs>? MensagemRecebida;
		public event EventHandler<MensagemExcluidaEventArgs>? MensagemExcluida;

		public bool Conectado => _connection?.State == HubConnectionState.Connected;

		public async Task ConectarAsync(Guid usuarioId)
		{
			if (Conectado) return;

			_connection = new HubConnectionBuilder()
				.WithUrl($"{ApiConfig.ServidorBaseUrl}/chathub")
				.WithAutomaticReconnect()
				.Build();

			_connection.On<JsonElement>("ReceberMensagem", json =>
			{
				MensagemRecebida?.Invoke(this, ParseMensagem(json));
			});

			_connection.On<JsonElement>("MensagemExcluida", json =>
			{
				var id = Guid.Parse(json.GetProperty("mensagemId").GetString()!);
				MensagemExcluida?.Invoke(this, new MensagemExcluidaEventArgs { MensagemId = id });
			});

			await _connection.StartAsync();
			await _connection.InvokeAsync("Entrar", usuarioId.ToString());
		}

		public Task<Guid> EnviarTextoAsync(Guid remetenteId, Guid destinatarioId, string texto)
			=> EnviarAsync(remetenteId, destinatarioId, TipoConteudoMensagem.Texto, texto, null, null, null);

		public Task<Guid> EnviarArquivoAsync(Guid remetenteId, Guid destinatarioId,
			TipoConteudoMensagem tipo, string caminhoArquivo, string? nomeArquivoOriginal,
			string legenda = "", int? duracaoSegundos = null)
			=> EnviarAsync(remetenteId, destinatarioId, tipo, legenda, caminhoArquivo, nomeArquivoOriginal, duracaoSegundos);

		private async Task<Guid> EnviarAsync(Guid remetenteId, Guid destinatarioId,
			TipoConteudoMensagem tipo, string conteudo, string? caminhoArquivo, string? nomeArquivoOriginal,
			int? duracaoSegundos)
		{
			if (!Conectado || _connection is null)
				throw new InvalidOperationException("Sem conexão com o servidor de chat.");

			var resultado = await _connection.InvokeAsync<JsonElement>("EnviarMensagem",
				remetenteId.ToString(), destinatarioId.ToString(),
				tipo.ToString(), conteudo, caminhoArquivo, nomeArquivoOriginal, duracaoSegundos);

			return Guid.Parse(resultado.GetProperty("id").GetString()!);
		}

		/// <summary>Envia um feedback do psicólogo a uma resposta de
		/// questionário específica — vai pro chat normal com o paciente,
		/// citando a pergunta/resposta originais.</summary>
		public async Task<Guid> EnviarFeedbackAsync(Guid remetenteId, Guid destinatarioId, Guid respostaId,
			TipoConteudoMensagem tipo, string conteudo, string? caminhoArquivo = null, string? nomeArquivoOriginal = null,
			int? duracaoSegundos = null)
		{
			if (!Conectado || _connection is null)
				throw new InvalidOperationException("Sem conexão com o servidor de chat.");

			var resultado = await _connection.InvokeAsync<JsonElement>("EnviarFeedback",
				remetenteId.ToString(), destinatarioId.ToString(), respostaId.ToString(),
				tipo.ToString(), conteudo, caminhoArquivo, nomeArquivoOriginal, duracaoSegundos);

			return Guid.Parse(resultado.GetProperty("id").GetString()!);
		}

		public async Task ExcluirMensagemAsync(Guid mensagemId, Guid usuarioId)
		{
			if (!Conectado || _connection is null) return;
			await _connection.InvokeAsync("ExcluirMensagem", mensagemId.ToString(), usuarioId.ToString());
		}

		/// <summary>Chamado ao abrir a conversa — marca as mensagens desse
		/// remetente como lidas, tirando elas da lista de notificações.</summary>
		public async Task MarcarComoLidasAsync(Guid remetenteId, Guid destinatarioId)
		{
			if (!Conectado || _connection is null) return;
			await _connection.InvokeAsync("MarcarComoLidas", remetenteId.ToString(), destinatarioId.ToString());
		}

		public async Task DesconectarAsync()
		{
			if (_connection is not null)
			{
				await _connection.StopAsync();
				await _connection.DisposeAsync();
				_connection = null;
			}
		}

		public async Task<List<MensagemRecebidaEventArgs>> ObterHistoricoAsync(Guid usuarioAId, Guid usuarioBId)
		{
			if (!Conectado || _connection is null)
				return new List<MensagemRecebidaEventArgs>();

			var resultado = await _connection.InvokeAsync<List<JsonElement>>(
				"ObterHistorico", usuarioAId.ToString(), usuarioBId.ToString());

			var lista = new List<MensagemRecebidaEventArgs>();
			foreach (var item in resultado)
				lista.Add(ParseMensagem(item));

			return lista;
		}

		private static MensagemRecebidaEventArgs ParseMensagem(JsonElement item)
		{
			var tipo = Enum.TryParse<TipoConteudoMensagem>(
				item.GetProperty("tipoConteudo").GetString(), out var t) ? t : TipoConteudoMensagem.Texto;

			return new MensagemRecebidaEventArgs
			{
				MensagemId = Guid.Parse(item.GetProperty("id").GetString()!),
				RemetenteId = Guid.Parse(item.GetProperty("remetenteId").GetString()!),
				DestinatarioId = Guid.Parse(item.GetProperty("destinatarioId").GetString()!),
				TipoConteudo = tipo,
				Conteudo = item.GetProperty("conteudo").GetString() ?? string.Empty,
				CaminhoArquivo = item.TryGetProperty("caminhoArquivo", out var ca) && ca.ValueKind != JsonValueKind.Null ? ca.GetString() : null,
				NomeArquivoOriginal = item.TryGetProperty("nomeArquivoOriginal", out var no) && no.ValueKind != JsonValueKind.Null ? no.GetString() : null,
				DuracaoSegundos = item.TryGetProperty("duracaoSegundos", out var ds) && ds.ValueKind == JsonValueKind.Number ? ds.GetInt32() : null,
				Excluida = item.TryGetProperty("excluida", out var ex) && ex.ValueKind == JsonValueKind.True,
				EnviadaEm = DateTime.Parse(item.GetProperty("enviadaEm").GetString()!).ToLocalTime(),
				RespostaId = item.TryGetProperty("respostaId", out var ri) && ri.ValueKind == JsonValueKind.String ? Guid.Parse(ri.GetString()!) : null,
				CitacaoTextoPergunta = item.TryGetProperty("citacaoTextoPergunta", out var ctp) && ctp.ValueKind == JsonValueKind.String ? ctp.GetString() : null,
				CitacaoTextoResposta = item.TryGetProperty("citacaoTextoResposta", out var ctr) && ctr.ValueKind == JsonValueKind.String ? ctr.GetString() : null
			};
		}
	}
}