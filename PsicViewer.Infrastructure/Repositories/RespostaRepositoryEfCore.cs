using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PsicViewer.Core.Entities;
using PsicViewer.Core.Interfaces;
using PsicViewer.Infrastructure.Persistencia;

namespace PsicViewer.Infrastructure.Repositories;

public class RespostaRepositoryEfCore : IRespostaRepository
{
	private readonly PsicViewerDbContext _db;

	public RespostaRepositoryEfCore(PsicViewerDbContext db)
	{
		_db = db;
	}

	public async Task SalvarAsync(Resposta resposta)
	{
		_db.Respostas.Add(resposta);
		await _db.SaveChangesAsync();
	}

	public async Task AtualizarAsync(Resposta resposta)
	{
		_db.Respostas.Update(resposta);
		await _db.SaveChangesAsync();
	}

	public async Task<Resposta?> ObterPorIdAsync(Guid id)
	{
		return await _db.Respostas.FirstOrDefaultAsync((Resposta r) => r.Id == id);
	}

	public async Task<Resposta?> ObterPorPerguntaPacienteEDataAsync(Guid perguntaId, Guid pacienteId, DateOnly data)
	{
		return await _db.Respostas.FirstOrDefaultAsync((Resposta r) => r.PerguntaId == perguntaId && r.PacienteId == pacienteId && r.Data == data);
	}

	public async Task<IReadOnlyList<Resposta>> ListarPorQuestionarioEPacienteAsync(Guid questionarioId, Guid pacienteId)
	{
		return await _db.Respostas.Where((Resposta r) => r.QuestionarioId == questionarioId && r.PacienteId == pacienteId).ToListAsync();
	}

	public async Task<IReadOnlyList<Resposta>> ListarPorQuestionarioAsync(Guid questionarioId)
	{
		return await _db.Respostas.Where((Resposta r) => r.QuestionarioId == questionarioId).ToListAsync();
	}

	public async Task<IReadOnlyList<Resposta>> ListarHistoricoPorPacienteAsync(Guid pacienteId)
	{
		DateOnly hoje = DateOnly.FromDateTime(DateTime.UtcNow);
		return await (from r in _db.Respostas
			where r.PacienteId == pacienteId && r.Data < hoje
			orderby r.Data descending, r.RespondidoEm descending
			select r).ToListAsync();
	}
}
