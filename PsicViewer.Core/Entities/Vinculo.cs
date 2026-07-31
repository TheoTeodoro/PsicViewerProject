using System;

namespace PsicViewer.Core.Entities;

public class Vinculo
{
	public Guid Id { get; private set; }

	public Guid PacienteId { get; private set; }

	public Guid PsicologoId { get; private set; }

	public StatusVinculo Status { get; private set; }

	public OrigemSolicitacao Origem { get; private set; }

	public DateTime SolicitadoEm { get; private set; }

	public DateTime? RespondidoEm { get; private set; }

	public bool PedidoVisualizado { get; private set; }

	public bool AceitoVisualizado { get; private set; }

	private Vinculo()
	{
	}

	public Vinculo(Guid pacienteId, Guid psicologoId, OrigemSolicitacao origem)
	{
		if (pacienteId == Guid.Empty)
		{
			throw new ArgumentException("Paciente inválido.", "pacienteId");
		}
		if (psicologoId == Guid.Empty)
		{
			throw new ArgumentException("Psicólogo inválido.", "psicologoId");
		}
		Id = Guid.NewGuid();
		PacienteId = pacienteId;
		PsicologoId = psicologoId;
		Origem = origem;
		Status = StatusVinculo.Pendente;
		SolicitadoEm = DateTime.UtcNow;
	}

	public void Aceitar()
	{
		if (Status != StatusVinculo.Pendente)
		{
			throw new InvalidOperationException("Só é possível aceitar uma solicitação pendente.");
		}
		Status = StatusVinculo.Aceito;
		RespondidoEm = DateTime.UtcNow;
	}

	public void Recusar()
	{
		if (Status != StatusVinculo.Pendente)
		{
			throw new InvalidOperationException("Só é possível recusar uma solicitação pendente.");
		}
		Status = StatusVinculo.Recusado;
		RespondidoEm = DateTime.UtcNow;
	}

	public void Encerrar()
	{
		if (Status != StatusVinculo.Aceito)
		{
			throw new InvalidOperationException("Só é possível encerrar um vínculo aceito.");
		}
		Status = StatusVinculo.Encerrado;
		RespondidoEm = DateTime.UtcNow;
	}

	public void MarcarPedidoVisualizado()
	{
		PedidoVisualizado = true;
	}

	public void MarcarAceitoVisualizado()
	{
		AceitoVisualizado = true;
	}
}
