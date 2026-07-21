using System;

namespace MauiApp1.Services
{
	public class VinculoDto
	{
		public Guid Id { get; set; }
		public string Status { get; set; } = string.Empty;   // "Pendente", "Aceito", "Recusado"
		public string Origem { get; set; } = string.Empty;   // "Paciente" ou "Psicologo" — quem iniciou
		public DateTime SolicitadoEm { get; set; }
		public DateTime? RespondidoEm { get; set; }
		public Guid ContatoId { get; set; }
		public string ContatoNome { get; set; } = string.Empty;
		public string? ContatoFotoUrl { get; set; }
		public string? ContatoCrp { get; set; }
	}
}