using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PsicViewer.Core.Entities;
using PsicViewer.Core.Interfaces;

namespace PsicViewer.Infrastructure.Repositories;

public class PacienteRepositoryInMemory : IPacienteRepository
{
	private readonly ConcurrentDictionary<Guid, Paciente> _dados = new ConcurrentDictionary<Guid, Paciente>();

	public Task<Paciente?> ObterPorIdAsync(Guid id)
	{
		_dados.TryGetValue(id, out Paciente paciente);
		return Task.FromResult(paciente);
	}

	public Task<Paciente?> ObterPorEmailAsync(string email)
	{
		return Task.FromResult(_dados.Values.FirstOrDefault((Paciente p) => p.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));
	}

	public Task SalvarAsync(Paciente paciente)
	{
		_dados[paciente.Id] = paciente;
		return Task.CompletedTask;
	}

	public Task AtualizarAsync(Paciente paciente)
	{
		_dados[paciente.Id] = paciente;
		return Task.CompletedTask;
	}

	public Task<IReadOnlyList<Paciente>> ListarTodosAsync()
	{
		return Task.FromResult((IReadOnlyList<Paciente>)_dados.Values.ToList());
	}
}
