using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PsicViewer.Core.Entities;
using PsicViewer.Core.Interfaces;
using PsicViewer.Infrastructure.Persistencia;

namespace PsicViewer.Infrastructure.Repositories;

public class MensagemRepositoryEfCore : IMensagemRepository
{
	private readonly PsicViewerDbContext _db;

	public MensagemRepositoryEfCore(PsicViewerDbContext db)
	{
		_db = db;
	}

	public async Task SalvarAsync(Mensagem mensagem)
	{
		_db.Mensagens.Add(mensagem);
		await _db.SaveChangesAsync();
	}

	public async Task<Mensagem?> ObterPorIdAsync(Guid id)
	{
		return await _db.Mensagens.FirstOrDefaultAsync((Mensagem m) => m.Id == id);
	}

	public async Task AtualizarAsync(Mensagem mensagem)
	{
		_db.Mensagens.Update(mensagem);
		await _db.SaveChangesAsync();
	}

	public async Task<IReadOnlyList<Mensagem>> ObterConversaAsync(Guid usuarioAId, Guid usuarioBId)
	{
		return await (from m in _db.Mensagens
			where (m.RemetenteId == usuarioAId && m.DestinatarioId == usuarioBId) || (m.RemetenteId == usuarioBId && m.DestinatarioId == usuarioAId)
			orderby m.EnviadaEm
			select m).ToListAsync();
	}

	public async Task<IReadOnlyList<Mensagem>> ObterNaoLidasAsync(Guid destinatarioId)
	{
		return await _db.Mensagens.Where((Mensagem m) => m.DestinatarioId == destinatarioId && !m.Lida && !m.Excluida).ToListAsync();
	}

	public async Task MarcarComoLidasAsync(Guid remetenteId, Guid destinatarioId)
	{
		foreach (Mensagem item in await _db.Mensagens.Where((Mensagem m) => m.RemetenteId == remetenteId && m.DestinatarioId == destinatarioId && !m.Lida).ToListAsync())
		{
			item.MarcarComoLida();
		}
		await _db.SaveChangesAsync();
	}
}
