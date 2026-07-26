using System;
using System.Collections.Generic;

namespace MauiApp1.Services
{
	public enum TipoUsuarioLogado
	{
		Nenhum,
		Paciente,
		Psicologo
	}

	public class SessaoUsuario
	{
		public Guid UsuarioId { get; private set; }
		public string Nome { get; private set; } = string.Empty;
		public string Email { get; private set; } = string.Empty;
		public string? FotoUrl { get; private set; }
		public TipoUsuarioLogado Tipo { get; private set; } = TipoUsuarioLogado.Nenhum;

		// Guarda quais notificações (identificadas por uma chave tipo
		// "{vinculoId}-pedido" ou "{vinculoId}-aceito") já foram vistas —
		// sem isso, o contador do sino e a tela de notificações mostrariam
		// tudo como "não lida" pra sempre. Vive só na sessão (RAM).
		private readonly HashSet<string> _notificacoesVistas = new();

		public void IniciarComoPaciente(Guid id, string nome, string email, string? fotoUrl)
		{
			UsuarioId = id;
			Nome = nome;
			Email = email;
			FotoUrl = fotoUrl;
			Tipo = TipoUsuarioLogado.Paciente;
		}

		public void IniciarComoPsicologo(Guid id, string nome, string email, string? fotoUrl)
		{
			UsuarioId = id;
			Nome = nome;
			Email = email;
			FotoUrl = fotoUrl;
			Tipo = TipoUsuarioLogado.Psicologo;
		}

		public void EncerrarSessao()
		{
			UsuarioId = Guid.Empty;
			Nome = string.Empty;
			Email = string.Empty;
			FotoUrl = null;
			Tipo = TipoUsuarioLogado.Nenhum;
			_notificacoesVistas.Clear();
		}

		public void AtualizarDadosBasicos(string nome, string email, string? fotoUrl)
		{
			Nome = nome;
			Email = email;
			FotoUrl = fotoUrl;
		}

		public bool NotificacaoVista(string chave) => _notificacoesVistas.Contains(chave);

		public void MarcarNotificacaoVista(string chave) => _notificacoesVistas.Add(chave);
	}
}