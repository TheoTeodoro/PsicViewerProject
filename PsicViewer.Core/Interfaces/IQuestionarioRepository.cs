using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PsicViewer.Core.Entities;

namespace PsicViewer.Core.Interfaces;

public interface IQuestionarioRepository
{
	Task SalvarAsync(Questionario questionario);

	Task<Questionario?> ObterPorIdAsync(Guid id);

	Task AtualizarAsync(Questionario questionario);

	Task<IReadOnlyList<Questionario>> ListarPorPsicologoAsync(Guid psicologoId);
}
