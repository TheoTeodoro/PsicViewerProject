using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PsicViewer.Core.Entities;
using PsicViewer.Core.Interfaces;

namespace PsicViewer.Infrastructure.Repositories;

public class PsicologoRepositoryInMemory : IPsicologoRepository
{
	private readonly ConcurrentDictionary<Guid, Psicologo> _dados = new ConcurrentDictionary<Guid, Psicologo>();

	public Task<Psicologo?> ObterPorIdAsync(Guid id)
	{
		_dados.TryGetValue(id, out Psicologo psicologo);
		return Task.FromResult(psicologo);
	}

	public Task<Psicologo?> ObterPorEmailAsync(string email)
	{
		return Task.FromResult(_dados.Values.FirstOrDefault((Psicologo p) => p.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));
	}

	public Task<Psicologo?> ObterPorCrpAsync(string crp)
	{
		return Task.FromResult(_dados.Values.FirstOrDefault((Psicologo p) => p.Crp.Equals(crp, StringComparison.OrdinalIgnoreCase)));
	}

	public Task SalvarAsync(Psicologo psicologo)
	{
		_dados[psicologo.Id] = psicologo;
		return Task.CompletedTask;
	}

	public Task AtualizarAsync(Psicologo psicologo)
	{
		_dados[psicologo.Id] = psicologo;
		return Task.CompletedTask;
	}

	public Task<IReadOnlyList<Psicologo>> ListarTodosAsync()
	{
		return Task.FromResult((IReadOnlyList<Psicologo>)_dados.Values.ToList());
	}
}
