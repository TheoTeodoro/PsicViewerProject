using System;

namespace MauiApp1.Services
{
	public class UsuarioDto
	{
		public Guid Id { get; set; }
		public string Nome { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string? Crp { get; set; }
		public string? Telefone { get; set; }
		public DateTime? DataNascimento { get; set; }
		public string? Genero { get; set; }
		public string? FotoUrl { get; set; }
		public string Tipo { get; set; } = string.Empty;
	}
}