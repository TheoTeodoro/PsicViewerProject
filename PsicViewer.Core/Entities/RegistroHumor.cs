using System;

namespace PsicViewer.Core.Entities;

public class RegistroHumor
{
	public const int NivelMinimo = 0;

	public const int NivelMaximo = 10;

	public Guid Id { get; private set; }

	public Guid PacienteId { get; private set; }

	public int Nivel { get; private set; }

	public string? Texto { get; private set; }

	public string? CaminhoAudio { get; private set; }

	public DateTime RegistradoEm { get; private set; }

	public string? FeedbackPsicologo { get; private set; }

	public DateTime? FeedbackEm { get; private set; }

	private RegistroHumor()
	{
	}

	internal RegistroHumor(Guid pacienteId, int nivel, string? texto, string? caminhoAudio)
	{
		if (nivel < 0 || nivel > 10)
		{
			throw new ArgumentOutOfRangeException("nivel", $"O nível de humor deve estar entre {0} e {10}.");
		}
		Id = Guid.NewGuid();
		PacienteId = pacienteId;
		Nivel = nivel;
		Texto = texto;
		CaminhoAudio = caminhoAudio;
		RegistradoEm = DateTime.UtcNow;
	}

	public void AdicionarFeedback(string feedback)
	{
		if (string.IsNullOrWhiteSpace(feedback))
		{
			throw new ArgumentException("O feedback não pode ser vazio.", "feedback");
		}
		FeedbackPsicologo = feedback;
		FeedbackEm = DateTime.UtcNow;
	}

	public string Classificar()
	{
		int nivel = Nivel;
		if (nivel <= 6)
		{
			if (nivel > 2)
			{
				if (nivel <= 4)
				{
					return "Baixo";
				}
				return "Neutro";
			}
			return "Muito baixo";
		}
		if (nivel <= 8)
		{
			return "Bom";
		}
		return "Ótimo";
	}
}
