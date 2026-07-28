using System;
using System.Collections.Generic;
namespace MauiApp1.Services
{
	public class QuestionarioParaPacienteDto
	{
		public Guid Id { get; set; }
		public string Titulo { get; set; } = string.Empty;
		public string DiasSemana { get; set; } = string.Empty;
		public int QuantidadePerguntas { get; set; }
		public int QuantidadeRespondidasHoje { get; set; }
	}
	public class PerguntaParaResponderDto
	{
		public Guid Id { get; set; }
		public string Tipo { get; set; } = string.Empty;
		public string Texto { get; set; } = string.Empty;
		public string? Opcoes { get; set; }

		// Preenchidos quando essa pergunta já foi respondida HOJE — a
		// tela de responder usa isso pra já abrir travada/preenchida em
		// vez de sempre em branco.
		public bool RespondidaHoje { get; set; }
		public int? ValorEscala { get; set; }
		public string? RespostaTexto { get; set; }
		public string? Observacao { get; set; }
		public string? AudioObservacao { get; set; }
	}
	public class QuestionarioParaResponderDto
	{
		public Guid Id { get; set; }
		public string Titulo { get; set; } = string.Empty;
		public List<PerguntaParaResponderDto> Perguntas { get; set; } = new();
	}
	public record RespostaParaEnviar(Guid PerguntaId, int? ValorEscala, string? RespostaTexto, string? Observacao, string? AudioObservacao);

	/// <summary>Contexto completo de uma resposta — pergunta original +
	/// o que o paciente respondeu — usado na tela de "Dar Feedback" do
	/// psicólogo.</summary>
	public class RespostaDetalheDto
	{
		public Guid RespostaId { get; set; }
		public Guid PacienteId { get; set; }
		public string PacienteNome { get; set; } = string.Empty;
		public string QuestionarioTitulo { get; set; } = string.Empty;
		public string PerguntaTexto { get; set; } = string.Empty;
		public int? ValorEscala { get; set; }
		public string? RespostaTexto { get; set; }
		public string? Observacao { get; set; }
		public string? AudioObservacao { get; set; }
		public DateTime RespondidoEm { get; set; }
	}

	/// <summary>Uma resposta já "arquivada" no Histórico do paciente —
	/// dia anterior a hoje.</summary>
	public class ItemHistoricoDto
	{
		public Guid RespostaId { get; set; }
		public Guid QuestionarioId { get; set; }
		public string Data { get; set; } = string.Empty; // "yyyy-MM-dd"
		public string QuestionarioTitulo { get; set; } = string.Empty;
		public string PerguntaTexto { get; set; } = string.Empty;
		public string TipoPergunta { get; set; } = string.Empty;
		public int? ValorEscala { get; set; }
		public string? RespostaTexto { get; set; }
		public string? Observacao { get; set; }
		public string? AudioObservacao { get; set; }
		public DateTime RespondidoEm { get; set; }
	}

	/// <summary>Uma pergunta dentro do detalhe de um questionário
	/// respondido num dia específico do histórico — Respondida=false
	/// quando essa pergunta não teve resposta naquele dia (o app mostra
	/// "Sem resposta" nesse caso).</summary>
	public class PerguntaHistoricoDetalheDto
	{
		public Guid Id { get; set; }
		public string Tipo { get; set; } = string.Empty;
		public string Texto { get; set; } = string.Empty;
		public string? Opcoes { get; set; }
		public bool Respondida { get; set; }
		public int? ValorEscala { get; set; }
		public string? RespostaTexto { get; set; }
		public string? Observacao { get; set; }
		public string? AudioObservacao { get; set; }
	}

	public class QuestionarioHistoricoDetalheDto
	{
		public Guid Id { get; set; }
		public string Titulo { get; set; } = string.Empty;
		public string Data { get; set; } = string.Empty;
		public List<PerguntaHistoricoDetalheDto> Perguntas { get; set; } = new();
	}

	/// <summary>Um ponto do gráfico de Relatório de Humor (RF22) — a
	/// média das respostas de Escala daquele dia.</summary>
	public class PontoHumorDto
	{
		public string Data { get; set; } = string.Empty; // "yyyy-MM-dd"
		public double MediaHumor { get; set; }
	}

	/// <summary>Um ponto da série de UMA pergunta específica — usado
	/// tanto pra pergunta de Escala (ValorEscala preenchido, vira a
	/// linha principal) quanto pra pergunta de destaque tipo Múltipla
	/// Escolha (RespostaTexto preenchido, vira os marcadores).</summary>
	public class PontoSerieDto
	{
		public string Data { get; set; } = string.Empty;
		public int? ValorEscala { get; set; }
		public string? RespostaTexto { get; set; }
		public string? Observacao { get; set; }
		public string? AudioObservacao { get; set; }
	}
}