using System;

namespace PsicViewer.Core.Entities;

public class Pergunta
{
	public Guid Id { get; private set; }

	public Guid QuestionarioId { get; private set; }

	public TipoPergunta Tipo { get; private set; }

	public string TextoPergunta { get; private set; }

	public string? Opcoes { get; private set; }

	public TimeSpan HorarioNotificacao { get; private set; }

	public int Ordem { get; private set; }

	public bool Ativa { get; private set; }

	private Pergunta()
	{
		TextoPergunta = string.Empty;
	}

	public Pergunta(Guid questionarioId, TipoPergunta tipo, string textoPergunta, TimeSpan horarioNotificacao, int ordem, string? opcoes = null)
	{
		if (questionarioId == Guid.Empty)
		{
			throw new ArgumentException("Questionário inválido.", "questionarioId");
		}
		if (string.IsNullOrWhiteSpace(textoPergunta))
		{
			throw new ArgumentException("O texto da pergunta é obrigatório.", "textoPergunta");
		}
		if (tipo == TipoPergunta.MultiplaEscolha && string.IsNullOrWhiteSpace(opcoes))
		{
			throw new ArgumentException("Perguntas de múltipla escolha precisam de opções.", "opcoes");
		}
		Id = Guid.NewGuid();
		QuestionarioId = questionarioId;
		Tipo = tipo;
		TextoPergunta = textoPergunta.Trim();
		Opcoes = opcoes?.Trim();
		HorarioNotificacao = horarioNotificacao;
		Ordem = ordem;
		Ativa = true;
	}

	public void Desativar()
	{
		Ativa = false;
	}

	public void Ativar()
	{
		Ativa = true;
	}

	public void AtualizarHorario(TimeSpan horarioNotificacao)
	{
		HorarioNotificacao = horarioNotificacao;
	}
}
