using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PsicViewer.Core.Entities;
using PsicViewer.Core.Interfaces;
using PsicViewer.Infrastructure.Persistencia;

namespace PsicViewer.Infrastructure.Repositories;

public class VinculoRepositoryEfCore : IVinculoRepository
{
	private readonly PsicViewerDbContext _db;

	public VinculoRepositoryEfCore(PsicViewerDbContext db)
	{
		_db = db;
	}

	public async Task SalvarAsync(Vinculo vinculo)
	{
		_db.Vinculos.Add(vinculo);
		await _db.SaveChangesAsync();
	}

	public async Task<Vinculo?> ObterPorIdAsync(Guid id)
	{
		return await _db.Vinculos.FirstOrDefaultAsync((Vinculo v) => v.Id == id);
	}

	public async Task<Vinculo?> ObterVinculoAtivoAsync(Guid pacienteId, Guid psicologoId)
	{
		return await _db.Vinculos.FirstOrDefaultAsync((Vinculo v) => v.PacienteId == pacienteId && v.PsicologoId == psicologoId && ((int)v.Status == 0 || (int)v.Status == 1));
	}

	public async Task<Vinculo?> ObterVinculoAtivoDoPacienteAsync(Guid pacienteId)
	{
		return await _db.Vinculos.FirstOrDefaultAsync((Vinculo v) => v.PacienteId == pacienteId && ((int)v.Status == 0 || (int)v.Status == 1));
	}

	public async Task AtualizarAsync(Vinculo vinculo)
	{
		_db.Vinculos.Update(vinculo);
		await _db.SaveChangesAsync();
	}

	public async Task<IReadOnlyList<Vinculo>> ListarPorPacienteAsync(Guid pacienteId)
	{
		return await _db.Vinculos.Where((Vinculo v) => v.PacienteId == pacienteId).ToListAsync();
	}

	public async Task<IReadOnlyList<Vinculo>> ListarPorPsicologoAsync(Guid psicologoId)
	{
		return await _db.Vinculos.Where((Vinculo v) => v.PsicologoId == psicologoId).ToListAsync();
	}
}
