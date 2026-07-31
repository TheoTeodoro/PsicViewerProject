using System;

namespace PsicViewer.Core.Entities;

public class Mensagem
{
	public Guid Id { get; private set; }

	public Guid RemetenteId { get; private set; }

	public Guid DestinatarioId { get; private set; }

	public TipoConteudoMensagem TipoConteudo { get; private set; }

	public string Conteudo { get; private set; }

	public string? CaminhoArquivo { get; private set; }

	public string? NomeArquivoOriginal { get; private set; }

	public int? DuracaoSegundos { get; private set; }

	public DateTime EnviadaEm { get; private set; }

	public bool Lida { get; private set; }

	public bool Excluida { get; private set; }

	public Guid? RespostaId { get; private set; }

	public string? CitacaoTextoPergunta { get; private set; }

	public string? CitacaoTextoResposta { get; private set; }

	public string? CitacaoQuestionarioTitulo { get; private set; }

	private Mensagem()
	{
		Conteudo = string.Empty;
	}

	public Mensagem(Guid remetenteId, Guid destinatarioId, string conteudo)
	{
		ValidarParticipantes(remetenteId, destinatarioId);
		if (string.IsNullOrWhiteSpace(conteudo))
		{
			throw new ArgumentException("A mensagem não pode ser vazia.", "conteudo");
		}
		Id = Guid.NewGuid();
		RemetenteId = remetenteId;
		DestinatarioId = destinatarioId;
		TipoConteudo = TipoConteudoMensagem.Texto;
		Conteudo = conteudo.Trim();
		EnviadaEm = DateTime.UtcNow;
	}

	public Mensagem(Guid remetenteId, Guid destinatarioId, TipoConteudoMensagem tipo, string caminhoArquivo, string? nomeArquivoOriginal, string? legenda = null, int? duracaoSegundos = null)
	{
		if (tipo == TipoConteudoMensagem.Texto)
		{
			throw new ArgumentException("Use o construtor de texto para TipoConteudoMensagem.Texto.", "tipo");
		}
		ValidarParticipantes(remetenteId, destinatarioId);
		if (string.IsNullOrWhiteSpace(caminhoArquivo))
		{
			throw new ArgumentException("Caminho do arquivo é obrigatório.", "caminhoArquivo");
		}
		Id = Guid.NewGuid();
		RemetenteId = remetenteId;
		DestinatarioId = destinatarioId;
		TipoConteudo = tipo;
		CaminhoArquivo = caminhoArquivo;
		NomeArquivoOriginal = nomeArquivoOriginal;
		DuracaoSegundos = duracaoSegundos;
		Conteudo = legenda?.Trim() ?? string.Empty;
		EnviadaEm = DateTime.UtcNow;
	}

	private static void ValidarParticipantes(Guid remetenteId, Guid destinatarioId)
	{
		if (remetenteId == Guid.Empty)
		{
			throw new ArgumentException("Remetente inválido.", "remetenteId");
		}
		if (destinatarioId == Guid.Empty)
		{
			throw new ArgumentException("Destinatário inválido.", "destinatarioId");
		}
	}

	public void MarcarComoLida()
	{
		Lida = true;
	}

	public void ExcluirConteudo()
	{
		Excluida = true;
		Conteudo = string.Empty;
		CaminhoArquivo = null;
		NomeArquivoOriginal = null;
	}

	public void DefinirCitacaoFeedback(Guid respostaId, string textoPergunta, string? textoResposta, string? questionarioTitulo)
	{
		RespostaId = respostaId;
		CitacaoTextoPergunta = textoPergunta;
		CitacaoTextoResposta = textoResposta;
		CitacaoQuestionarioTitulo = questionarioTitulo;
	}
}
