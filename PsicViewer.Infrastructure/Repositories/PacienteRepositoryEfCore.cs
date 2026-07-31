using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PsicViewer.Core.Entities;
using PsicViewer.Core.Interfaces;
using PsicViewer.Infrastructure.Persistencia;

namespace PsicViewer.Infrastructure.Repositories;

public class PacienteRepositoryEfCore : IPacienteRepository
{
	private readonly PsicViewerDbContext _db;

	public PacienteRepositoryEfCore(PsicViewerDbContext db)
	{
		_db = db;
	}

	public async Task<Paciente?> ObterPorIdAsync(Guid id)
	{
		return await _db.Pacientes.FirstOrDefaultAsync((Paciente p) => p.Id == id);
	}

	public async Task<Paciente?> ObterPorEmailAsync(string email)
	{
		return await _db.Pacientes.FirstOrDefaultAsync((Paciente p) => p.Email == email.Trim().ToLower());
	}

	public async Task SalvarAsync(Paciente paciente)
	{
		_db.Pacientes.Add(paciente);
		await _db.SaveChangesAsync();
	}

	public async Task AtualizarAsync(Paciente paciente)
	{
		_db.Pacientes.Update(paciente);
		await _db.SaveChangesAsync();
	}

	public async Task<IReadOnlyList<Paciente>> ListarTodosAsync()
	{
		return await _db.Pacientes.ToListAsync();
	}
}
