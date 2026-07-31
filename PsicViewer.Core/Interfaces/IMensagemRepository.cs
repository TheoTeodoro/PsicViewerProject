using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PsicViewer.Core.Entities;

namespace PsicViewer.Core.Interfaces;

public interface IMensagemRepository
{
	Task SalvarAsync(Mensagem mensagem);

	Task<Mensagem?> ObterPorIdAsync(Guid id);

	Task AtualizarAsync(Mensagem mensagem);

	Task<IReadOnlyList<Mensagem>> ObterConversaAsync(Guid usuarioAId, Guid usuarioBId);

	Task<IReadOnlyList<Mensagem>> ObterNaoLidasAsync(Guid destinatarioId);

	Task MarcarComoLidasAsync(Guid remetenteId, Guid destinatarioId);
}
