using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using PsicViewer.Core.Entities;
using PsicViewer.Core.Interfaces;

namespace PsicViewer.Api.Hubs;

public class ChatHub : Hub
{
	private readonly IMensagemRepository _mensagens;

	private readonly IRespostaRepository _respostas;

	private readonly IQuestionarioRepository _questionarios;

	private readonly IHostEnvironment _env;

	private static readonly ConcurrentDictionary<string, Guid> _conexoes = new ConcurrentDictionary<string, Guid>();

	public ChatHub(IMensagemRepository mensagens, IRespostaRepository respostas, IQuestionarioRepository questionarios, IHostEnvironment env)
	{
		_mensagens = mensagens;
		_respostas = respostas;
		_questionarios = questionarios;
		_env = env;
	}

	public async Task Entrar(string usuarioId)
	{
		if (Guid.TryParse(usuarioId, out var id))
		{
			_conexoes[base.Context.ConnectionId] = id;
			await base.Groups.AddToGroupAsync(base.Context.ConnectionId, usuarioId);
		}
	}

	public async Task<object> EnviarMensagem(string remetenteId, string destinatarioId, string tipoConteudo, string conteudo, string? caminhoArquivo, string? nomeArquivoOriginal, int? duracaoSegundos)
	{
		if (!Guid.TryParse(remetenteId, out var remetenteGuid) || !Guid.TryParse(destinatarioId, out var destinatarioGuid))
		{
			throw new HubException("Remetente ou destinatário inválido.");
		}
		if (!Enum.TryParse<TipoConteudoMensagem>(tipoConteudo, out var tipo))
		{
			tipo = TipoConteudoMensagem.Texto;
		}
		Mensagem mensagem = ((tipo == TipoConteudoMensagem.Texto) ? new Mensagem(remetenteGuid, destinatarioGuid, conteudo) : new Mensagem(remetenteGuid, destinatarioGuid, tipo, caminhoArquivo, nomeArquivoOriginal, conteudo, duracaoSegundos));
		await _mensagens.SalvarAsync(mensagem);
		object payload = ParaPayload(mensagem);
		await base.Clients.Group(destinatarioId).SendAsync("ReceberMensagem", payload);
		return payload;
	}

	public async Task<object> EnviarFeedback(string remetenteId, string destinatarioId, string respostaId, string tipoConteudo, string conteudo, string? caminhoArquivo, string? nomeArquivoOriginal, int? duracaoSegundos)
	{
		if (!Guid.TryParse(remetenteId, out var remetenteGuid) || !Guid.TryParse(destinatarioId, out var destinatarioGuid) || !Guid.TryParse(respostaId, out var respostaGuid))
		{
			throw new HubException("Dados inválidos pra enviar o feedback.");
		}
		Resposta resposta = await _respostas.ObterPorIdAsync(respostaGuid);
		if (resposta == null)
		{
			throw new HubException("Resposta não encontrada.");
		}
		Questionario questionario = await _questionarios.ObterPorIdAsync(resposta.QuestionarioId);
		Pergunta pergunta = questionario?.Perguntas.FirstOrDefault((Pergunta p) => p.Id == resposta.PerguntaId);
		if (!Enum.TryParse<TipoConteudoMensagem>(tipoConteudo, out var tipo))
		{
			tipo = TipoConteudoMensagem.Texto;
		}
		Mensagem mensagem = ((tipo == TipoConteudoMensagem.Texto) ? new Mensagem(remetenteGuid, destinatarioGuid, conteudo) : new Mensagem(remetenteGuid, destinatarioGuid, tipo, caminhoArquivo, nomeArquivoOriginal, conteudo, duracaoSegundos));
		mensagem.DefinirCitacaoFeedback(resposta.Id, pergunta?.TextoPergunta ?? string.Empty, FormatarTextoResposta(resposta), questionario?.Titulo);
		await _mensagens.SalvarAsync(mensagem);
		resposta.MarcarComFeedback();
		await _respostas.AtualizarAsync(resposta);
		object payload = ParaPayload(mensagem);
		await base.Clients.Group(destinatarioId).SendAsync("ReceberMensagem", payload);
		return payload;
	}

	private static string FormatarTextoResposta(Resposta resposta)
	{
		if (!string.IsNullOrWhiteSpace(resposta.RespostaTexto))
		{
			return resposta.RespostaTexto;
		}
		int? valorEscala = resposta.ValorEscala;
		if (valorEscala.HasValue)
		{
			int valor = valorEscala.GetValueOrDefault();
			return $"Nível {valor}";
		}
		return string.Empty;
	}

	public async Task ExcluirMensagem(string mensagemId, string usuarioId)
	{
		if (!Guid.TryParse(mensagemId, out var msgId) || !Guid.TryParse(usuarioId, out var userId))
		{
			return;
		}
		Mensagem mensagem = await _mensagens.ObterPorIdAsync(msgId);
		if (mensagem == null || mensagem.RemetenteId != userId)
		{
			return;
		}
		if (!string.IsNullOrWhiteSpace(mensagem.CaminhoArquivo))
		{
			string nomeArquivo = Path.GetFileName(mensagem.CaminhoArquivo);
			string caminhoFisico = Path.Combine(_env.ContentRootPath, "UploadedFiles", nomeArquivo);
			if (File.Exists(caminhoFisico))
			{
				File.Delete(caminhoFisico);
			}
		}
		mensagem.ExcluirConteudo();
		await _mensagens.AtualizarAsync(mensagem);
		var payload = new
		{
			mensagemId = mensagem.Id.ToString()
		};
		await base.Clients.Group(mensagem.RemetenteId.ToString()).SendAsync("MensagemExcluida", payload);
		await base.Clients.Group(mensagem.DestinatarioId.ToString()).SendAsync("MensagemExcluida", payload);
	}

	public async Task<object[]> ObterHistorico(string usuarioAId, string usuarioBId)
	{
		if (!Guid.TryParse(usuarioAId, out var a) || !Guid.TryParse(usuarioBId, out var b))
		{
			return Array.Empty<object>();
		}
		return (await _mensagens.ObterConversaAsync(a, b)).Select(ParaPayload).ToArray();
	}

	private static object ParaPayload(Mensagem m)
	{
		return new
		{
			id = m.Id.ToString(),
			remetenteId = m.RemetenteId.ToString(),
			destinatarioId = m.DestinatarioId.ToString(),
			tipoConteudo = m.TipoConteudo.ToString(),
			conteudo = m.Conteudo,
			caminhoArquivo = m.CaminhoArquivo,
			nomeArquivoOriginal = m.NomeArquivoOriginal,
			duracaoSegundos = m.DuracaoSegundos,
			excluida = m.Excluida,
			enviadaEm = m.EnviadaEm.ToString("O"),
			respostaId = m.RespostaId?.ToString(),
			citacaoTextoPergunta = m.CitacaoTextoPergunta,
			citacaoTextoResposta = m.CitacaoTextoResposta,
			citacaoQuestionarioTitulo = m.CitacaoQuestionarioTitulo
		};
	}

	public async Task MarcarComoLidas(string remetenteId, string destinatarioId)
	{
		if (Guid.TryParse(remetenteId, out var r) && Guid.TryParse(destinatarioId, out var d))
		{
			await _mensagens.MarcarComoLidasAsync(r, d);
		}
	}

	public override Task OnDisconnectedAsync(Exception? exception)
	{
		_conexoes.TryRemove(base.Context.ConnectionId, out var _);
		return base.OnDisconnectedAsync(exception);
	}
}
