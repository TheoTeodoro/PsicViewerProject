using System;
using System.Collections.Generic;

namespace PsicViewer.Core.Entities;

public class Psicologo
{
	private readonly List<Paciente> _pacientes = new List<Paciente>();

	public Guid Id { get; private set; }

	public string Nome { get; private set; }

	public string Email { get; private set; }

	public string SenhaHash { get; private set; }

	public string Crp { get; private set; }

	public string? Telefone { get; private set; }

	public DateTime? DataNascimento { get; private set; }

	public GeneroUsuario? Genero { get; private set; }

	public string? FotoUrl { get; private set; }

	public bool Ativo { get; private set; }

	public DateTime CriadoEm { get; private set; }

	public IReadOnlyCollection<Paciente> Pacientes => _pacientes.AsReadOnly();

	private Psicologo()
	{
	}

	public Psicologo(string nome, string email, string senhaHash, string crp)
	{
		if (string.IsNullOrWhiteSpace(nome))
		{
			throw new ArgumentException("Nome é obrigatório.", "nome");
		}
		if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
		{
			throw new ArgumentException("E-mail inválido.", "email");
		}
		if (string.IsNullOrWhiteSpace(crp))
		{
			throw new ArgumentException("CRP é obrigatório para o perfil de psicólogo.", "crp");
		}
		Id = Guid.NewGuid();
		Nome = nome.Trim();
		Email = email.Trim().ToLowerInvariant();
		SenhaHash = senhaHash;
		Crp = crp.Trim();
		Ativo = true;
		CriadoEm = DateTime.UtcNow;
	}

	public void VincularPaciente(Paciente paciente)
	{
		if (paciente == null)
		{
			throw new ArgumentNullException("paciente");
		}
		if (!_pacientes.Exists((Paciente p) => p.Id == paciente.Id))
		{
			_pacientes.Add(paciente);
		}
	}

	public void Desativar(string motivo)
	{
		if (string.IsNullOrWhiteSpace(motivo))
		{
			throw new ArgumentException("O motivo do encerramento da conta é obrigatório.", "motivo");
		}
		Ativo = false;
	}

	public void AtualizarDados(string nome, string email, string? telefone, DateTime? dataNascimento, GeneroUsuario? genero, string crp)
	{
		if (string.IsNullOrWhiteSpace(nome))
		{
			throw new ArgumentException("Nome é obrigatório.", "nome");
		}
		if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
		{
			throw new ArgumentException("E-mail inválido.", "email");
		}
		if (string.IsNullOrWhiteSpace(crp))
		{
			throw new ArgumentException("CRP é obrigatório.", "crp");
		}
		Nome = nome.Trim();
		Email = email.Trim().ToLowerInvariant();
		Telefone = (string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim());
		DataNascimento = dataNascimento;
		Genero = genero;
		Crp = crp.Trim();
	}

	public void AtualizarFoto(string? caminhoFoto)
	{
		FotoUrl = (string.IsNullOrWhiteSpace(caminhoFoto) ? null : caminhoFoto);
	}
}
