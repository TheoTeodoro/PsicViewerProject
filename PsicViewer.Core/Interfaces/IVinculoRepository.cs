using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PsicViewer.Core.Entities;

namespace PsicViewer.Core.Interfaces;

public interface IVinculoRepository
{
	Task SalvarAsync(Vinculo vinculo);

	Task<Vinculo?> ObterPorIdAsync(Guid id);

	Task<Vinculo?> ObterVinculoAtivoAsync(Guid pacienteId, Guid psicologoId);

	Task<Vinculo?> ObterVinculoAtivoDoPacienteAsync(Guid pacienteId);

	Task AtualizarAsync(Vinculo vinculo);

	Task<IReadOnlyList<Vinculo>> ListarPorPacienteAsync(Guid pacienteId);

	Task<IReadOnlyList<Vinculo>> ListarPorPsicologoAsync(Guid psicologoId);
}
