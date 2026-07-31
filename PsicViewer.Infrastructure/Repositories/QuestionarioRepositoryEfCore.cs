using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PsicViewer.Core.Entities;
using PsicViewer.Core.Interfaces;
using PsicViewer.Infrastructure.Persistencia;

namespace PsicViewer.Infrastructure.Repositories;

public class QuestionarioRepositoryEfCore : IQuestionarioRepository
{
	private readonly PsicViewerDbContext _db;

	public QuestionarioRepositoryEfCore(PsicViewerDbContext db)
	{
		_db = db;
	}

	public async Task SalvarAsync(Questionario questionario)
	{
		_db.Questionarios.Add(questionario);
		await _db.SaveChangesAsync();
	}

	public async Task<Questionario?> ObterPorIdAsync(Guid id)
	{
		return await _db.Questionarios.Include((Questionario q) => q.Perguntas).FirstOrDefaultAsync((Questionario q) => q.Id == id);
	}

	public async Task AtualizarAsync(Questionario questionario)
	{
		foreach (Pergunta pergunta in questionario.Perguntas)
		{
			if (_db.Entry(pergunta).State == EntityState.Detached)
			{
				_db.Perguntas.Add(pergunta);
			}
		}
		await _db.SaveChangesAsync();
	}

	public async Task<IReadOnlyList<Questionario>> ListarPorPsicologoAsync(Guid psicologoId)
	{
		return await (from q in _db.Questionarios.Include((Questionario q) => q.Perguntas)
			where q.PsicologoId == psicologoId
			orderby q.CriadoEm descending
			select q).ToListAsync();
	}
}
