using System;
using System.Collections.Generic;
using System.Linq;

namespace PsicViewer.Core.Entities;

public class Questionario
{
	private readonly List<Pergunta> _perguntas = new List<Pergunta>();

	public Guid Id { get; private set; }

	public Guid PsicologoId { get; private set; }

	public string Titulo { get; private set; }

	public StatusQuestionario Status { get; private set; }

	public DateTime CriadoEm { get; private set; }

	public IReadOnlyCollection<Pergunta> Perguntas => _perguntas.AsReadOnly();

	private Questionario()
	{
		Titulo = string.Empty;
	}

	public Questionario(Guid psicologoId, string titulo)
	{
		if (psicologoId == Guid.Empty)
		{
			throw new ArgumentException("Psicólogo inválido.", "psicologoId");
		}
		if (string.IsNullOrWhiteSpace(titulo))
		{
			throw new ArgumentException("O título do questionário é obrigatório.", "titulo");
		}
		Id = Guid.NewGuid();
		PsicologoId = psicologoId;
		Titulo = titulo.Trim();
		Status = StatusQuestionario.Ativo;
		CriadoEm = DateTime.UtcNow;
	}

	public Pergunta AdicionarPergunta(TipoPergunta tipo, string textoPergunta, TimeSpan horarioNotificacao, string? opcoes = null)
	{
		Pergunta pergunta = new Pergunta(Id, tipo, textoPergunta, horarioNotificacao, _perguntas.Count, opcoes);
		_perguntas.Add(pergunta);
		return pergunta;
	}

	private Pergunta ObterPerguntaOuFalhar(Guid perguntaId)
	{
		return _perguntas.FirstOrDefault((Pergunta p) => p.Id == perguntaId) ?? throw new ArgumentException("Essa pergunta não pertence a esse questionário.", "perguntaId");
	}

	public void DesativarPergunta(Guid perguntaId)
	{
		ObterPerguntaOuFalhar(perguntaId).Desativar();
	}

	public void AtivarPergunta(Guid perguntaId)
	{
		ObterPerguntaOuFalhar(perguntaId).Ativar();
	}

	public void AtualizarHorarioPergunta(Guid perguntaId, TimeSpan horario)
	{
		ObterPerguntaOuFalhar(perguntaId).AtualizarHorario(horario);
	}

	public void RemoverPergunta(Guid perguntaId)
	{
		_perguntas.Remove(ObterPerguntaOuFalhar(perguntaId));
	}

	public void AtualizarTitulo(string titulo)
	{
		if (string.IsNullOrWhiteSpace(titulo))
		{
			throw new ArgumentException("O título do questionário é obrigatório.", "titulo");
		}
		Titulo = titulo.Trim();
	}

	public void Arquivar()
	{
		Status = StatusQuestionario.Arquivado;
	}

	public void Ativar()
	{
		Status = StatusQuestionario.Ativo;
	}
}
