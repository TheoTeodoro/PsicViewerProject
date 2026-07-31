using System;

namespace PsicViewer.Core.Entities;

public class QuestionarioPaciente
{
	public static readonly string[] TodosOsDias = new string[7] { "Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sab" };

	public Guid Id { get; private set; }

	public Guid QuestionarioId { get; private set; }

	public Guid PacienteId { get; private set; }

	public string DiasSemana { get; private set; }

	public DateTime VinculadoEm { get; private set; }

	private QuestionarioPaciente()
	{
		DiasSemana = string.Empty;
	}

	public QuestionarioPaciente(Guid questionarioId, Guid pacienteId, string? diasSemana = null)
	{
		if (questionarioId == Guid.Empty)
		{
			throw new ArgumentException("Questionário inválido.", "questionarioId");
		}
		if (pacienteId == Guid.Empty)
		{
			throw new ArgumentException("Paciente inválido.", "pacienteId");
		}
		Id = Guid.NewGuid();
		QuestionarioId = questionarioId;
		PacienteId = pacienteId;
		DiasSemana = (string.IsNullOrWhiteSpace(diasSemana) ? string.Join(",", TodosOsDias) : diasSemana);
		VinculadoEm = DateTime.UtcNow;
	}

	public void AtualizarDiasSemana(string? diasSemana)
	{
		DiasSemana = (string.IsNullOrWhiteSpace(diasSemana) ? string.Join(",", TodosOsDias) : diasSemana);
	}
}
