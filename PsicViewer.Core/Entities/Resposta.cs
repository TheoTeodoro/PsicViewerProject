using System;

namespace PsicViewer.Core.Entities;

public class Resposta
{
	public Guid Id { get; private set; }

	public Guid PerguntaId { get; private set; }

	public Guid QuestionarioId { get; private set; }

	public Guid PacienteId { get; private set; }

	public DateOnly Data { get; private set; }

	public int? ValorEscala { get; private set; }

	public string? RespostaTexto { get; private set; }

	public string? Observacao { get; private set; }

	public string? AudioObservacao { get; private set; }

	public bool Visualizada { get; private set; }

	public bool TemFeedback { get; private set; }

	public DateTime RespondidoEm { get; private set; }

	private Resposta()
	{
	}

	public Resposta(Guid perguntaId, Guid questionarioId, Guid pacienteId, int? valorEscala, string? respostaTexto, string? observacao = null, string? audioObservacao = null)
	{
		if (perguntaId == Guid.Empty)
		{
			throw new ArgumentException("Pergunta inválida.", "perguntaId");
		}
		if (questionarioId == Guid.Empty)
		{
			throw new ArgumentException("Questionário inválido.", "questionarioId");
		}
		if (pacienteId == Guid.Empty)
		{
			throw new ArgumentException("Paciente inválido.", "pacienteId");
		}
		if (!valorEscala.HasValue && string.IsNullOrWhiteSpace(respostaTexto))
		{
			throw new ArgumentException("A resposta precisa ter algum conteúdo.");
		}
		Id = Guid.NewGuid();
		PerguntaId = perguntaId;
		QuestionarioId = questionarioId;
		PacienteId = pacienteId;
		Data = DateOnly.FromDateTime(DateTime.UtcNow);
		ValorEscala = valorEscala;
		RespostaTexto = respostaTexto?.Trim();
		Observacao = (string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim());
		AudioObservacao = audioObservacao;
		RespondidoEm = DateTime.UtcNow;
	}

	public void AtualizarResposta(int? valorEscala, string? respostaTexto, string? observacao, string? audioObservacao)
	{
		if (!valorEscala.HasValue && string.IsNullOrWhiteSpace(respostaTexto))
		{
			throw new ArgumentException("A resposta precisa ter algum conteúdo.");
		}
		ValorEscala = valorEscala;
		RespostaTexto = respostaTexto?.Trim();
		Observacao = (string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim());
		AudioObservacao = audioObservacao;
		RespondidoEm = DateTime.UtcNow;
		Visualizada = false;
	}

	public void MarcarVisualizada()
	{
		Visualizada = true;
	}

	public void MarcarComFeedback()
	{
		TemFeedback = true;
	}
}
