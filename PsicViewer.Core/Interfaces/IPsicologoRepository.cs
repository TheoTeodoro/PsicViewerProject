using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PsicViewer.Core.Entities;

namespace PsicViewer.Core.Interfaces;

public interface IPsicologoRepository
{
	Task<Psicologo?> ObterPorIdAsync(Guid id);

	Task<Psicologo?> ObterPorEmailAsync(string email);

	Task<Psicologo?> ObterPorCrpAsync(string crp);

	Task SalvarAsync(Psicologo psicologo);

	Task AtualizarAsync(Psicologo psicologo);

	Task<IReadOnlyList<Psicologo>> ListarTodosAsync();
}
