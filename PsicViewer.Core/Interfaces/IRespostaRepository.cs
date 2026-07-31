using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PsicViewer.Core.Entities;

namespace PsicViewer.Core.Interfaces;

public interface IRespostaRepository
{
	Task SalvarAsync(Resposta resposta);

	Task AtualizarAsync(Resposta resposta);

	Task<Resposta?> ObterPorIdAsync(Guid id);

	Task<Resposta?> ObterPorPerguntaPacienteEDataAsync(Guid perguntaId, Guid pacienteId, DateOnly data);

	Task<IReadOnlyList<Resposta>> ListarPorQuestionarioEPacienteAsync(Guid questionarioId, Guid pacienteId);

	Task<IReadOnlyList<Resposta>> ListarPorQuestionarioAsync(Guid questionarioId);

	Task<IReadOnlyList<Resposta>> ListarHistoricoPorPacienteAsync(Guid pacienteId);
}
