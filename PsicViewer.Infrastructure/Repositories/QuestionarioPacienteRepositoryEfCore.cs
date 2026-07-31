using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PsicViewer.Core.Entities;
using PsicViewer.Core.Interfaces;
using PsicViewer.Infrastructure.Persistencia;

namespace PsicViewer.Infrastructure.Repositories;

public class QuestionarioPacienteRepositoryEfCore : IQuestionarioPacienteRepository
{
	private readonly PsicViewerDbContext _db;

	public QuestionarioPacienteRepositoryEfCore(PsicViewerDbContext db)
	{
		_db = db;
	}

	public async Task SalvarAsync(QuestionarioPaciente vinculo)
	{
		_db.QuestionarioPacientes.Add(vinculo);
		await _db.SaveChangesAsync();
	}

	public async Task<IReadOnlyList<QuestionarioPaciente>> ListarPorQuestionarioAsync(Guid questionarioId)
	{
		return await _db.QuestionarioPacientes.Where((QuestionarioPaciente x) => x.QuestionarioId == questionarioId).ToListAsync();
	}

	public async Task<IReadOnlyList<QuestionarioPaciente>> ListarPorPacienteAsync(Guid pacienteId)
	{
		return await _db.QuestionarioPacientes.Where((QuestionarioPaciente x) => x.PacienteId == pacienteId).ToListAsync();
	}

	public async Task RemoverAsync(Guid questionarioId, Guid pacienteId)
	{
		QuestionarioPaciente vinculo = await _db.QuestionarioPacientes.FirstOrDefaultAsync((QuestionarioPaciente x) => x.QuestionarioId == questionarioId && x.PacienteId == pacienteId);
		if (vinculo != null)
		{
			_db.QuestionarioPacientes.Remove(vinculo);
			await _db.SaveChangesAsync();
		}
	}
}
