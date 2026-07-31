using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PsicViewer.Core.Entities;

namespace PsicViewer.Core.Interfaces;

public interface IQuestionarioPacienteRepository
{
	Task SalvarAsync(QuestionarioPaciente vinculo);

	Task<IReadOnlyList<QuestionarioPaciente>> ListarPorQuestionarioAsync(Guid questionarioId);

	Task<IReadOnlyList<QuestionarioPaciente>> ListarPorPacienteAsync(Guid pacienteId);

	Task RemoverAsync(Guid questionarioId, Guid pacienteId);
}
