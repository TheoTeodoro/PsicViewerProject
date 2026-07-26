using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp1.Services
{
	public class QuestionarioDto
	{
		public Guid Id { get; set; }
		public string Titulo { get; set; } = string.Empty;
		public string Status { get; set; } = string.Empty; // "Ativo" ou "Arquivado"
		public DateTime CriadoEm { get; set; }
		public int QuantidadePerguntas { get; set; }
	}
}