using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PsicViewer.Core.Entities;
using PsicViewer.Core.Interfaces;
using PsicViewer.Infrastructure.Persistencia;

namespace PsicViewer.Infrastructure.Repositories;

public class PsicologoRepositoryEfCore : IPsicologoRepository
{
	private readonly PsicViewerDbContext _db;

	public PsicologoRepositoryEfCore(PsicViewerDbContext db)
	{
		_db = db;
	}

	public async Task<Psicologo?> ObterPorIdAsync(Guid id)
	{
		return await _db.Psicologos.FirstOrDefaultAsync((Psicologo p) => p.Id == id);
	}

	public async Task<Psicologo?> ObterPorEmailAsync(string email)
	{
		return await _db.Psicologos.FirstOrDefaultAsync((Psicologo p) => p.Email == email.Trim().ToLower());
	}

	public async Task<Psicologo?> ObterPorCrpAsync(string crp)
	{
		return await _db.Psicologos.FirstOrDefaultAsync((Psicologo p) => p.Crp == crp.Trim());
	}

	public async Task SalvarAsync(Psicologo psicologo)
	{
		_db.Psicologos.Add(psicologo);
		await _db.SaveChangesAsync();
	}

	public async Task AtualizarAsync(Psicologo psicologo)
	{
		_db.Psicologos.Update(psicologo);
		await _db.SaveChangesAsync();
	}

	public async Task<IReadOnlyList<Psicologo>> ListarTodosAsync()
	{
		return await _db.Psicologos.ToListAsync();
	}
}
