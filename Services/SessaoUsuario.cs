using System;

namespace MauiApp1.Services
{
	public enum TipoUsuarioLogado
	{
		Nenhum,
		Paciente,
		Psicologo
	}

	/// <summary>
	/// Guarda quem está logado durante a sessão do app (em memória).
	/// Temporário: quando existir IAuthService com JWT (Infrastructure),
	/// isso deve ser substituído por um token validado, não só um objeto
	/// solto na RAM.
	/// </summary>
	public class SessaoUsuario
	{
		public Guid UsuarioId { get; private set; }
		public string Nome { get; private set; } = string.Empty;
		public string Email { get; private set; } = string.Empty;
		public string? FotoUrl { get; private set; }
		public TipoUsuarioLogado Tipo { get; private set; } = TipoUsuarioLogado.Nenhum;

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
		}

		/// <summary>Atualiza nome/e-mail/foto em cache depois de uma edição
		/// de perfil — sem isso, a Home continuaria mostrando os dados
		/// antigos até o próximo login.</summary>
		public void AtualizarDadosBasicos(string nome, string email, string? fotoUrl)
		{
			Nome = nome;
			Email = email;
			FotoUrl = fotoUrl;
		}
	}
}
