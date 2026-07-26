using System;
using System.Collections.Generic;
namespace MauiApp1.Services
{
	public class PerguntaEdicaoDto
	{
		public Guid Id { get; set; }
		public string Tipo { get; set; } = string.Empty;
		public string Texto { get; set; } = string.Empty;
		public string? Opcoes { get; set; }
		public string Horario { get; set; } = string.Empty;
		public bool Ativa { get; set; }
	}
	public class PacienteVinculadoDto
	{
		public Guid PacienteId { get; set; }
		public string Nome { get; set; } = string.Empty;
		public string DiasSemana { get; set; } = string.Empty;
	}
	public class QuestionarioEdicaoDto
	{
		public Guid Id { get; set; }
		public string Titulo { get; set; } = string.Empty;
		public string Status { get; set; } = string.Empty;
		public List<PerguntaEdicaoDto> Perguntas { get; set; } = new();
		public List<PacienteVinculadoDto> Pacientes { get; set; } = new();
	}
}