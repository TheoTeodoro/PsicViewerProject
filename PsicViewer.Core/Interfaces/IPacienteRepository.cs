using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PsicViewer.Core.Entities;

namespace PsicViewer.Core.Interfaces;

public interface IPacienteRepository
{
	Task<Paciente?> ObterPorIdAsync(Guid id);

	Task<Paciente?> ObterPorEmailAsync(string email);

	Task SalvarAsync(Paciente paciente);

	Task AtualizarAsync(Paciente paciente);

	Task<IReadOnlyList<Paciente>> ListarTodosAsync();
}
