using System;
using System.Collections.Generic;

namespace PsicViewer.Core.Entities;

public class Paciente
{
	private readonly List<RegistroHumor> _registrosHumor = new List<RegistroHumor>();

	public Guid Id { get; private set; }

	public string Nome { get; private set; }

	public string Email { get; private set; }

	public string SenhaHash { get; private set; }

	public string? Telefone { get; private set; }

	public DateTime? DataNascimento { get; private set; }

	public GeneroUsuario? Genero { get; private set; }

	public string? FotoUrl { get; private set; }

	public Guid? PsicologoId { get; private set; }

	public bool Ativo { get; private set; }

	public DateTime CriadoEm { get; private set; }

	public IReadOnlyCollection<RegistroHumor> RegistrosHumor => _registrosHumor.AsReadOnly();

	private Paciente()
	{
	}

	public Paciente(string nome, string email, string senhaHash)
	{
		if (string.IsNullOrWhiteSpace(nome))
		{
			throw new ArgumentException("Nome é obrigatório.", "nome");
		}
		if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
		{
			throw new ArgumentException("E-mail inválido.", "email");
		}
		Id = Guid.NewGuid();
		Nome = nome.Trim();
		Email = email.Trim().ToLowerInvariant();
		SenhaHash = senhaHash;
		Ativo = true;
		CriadoEm = DateTime.UtcNow;
	}

	public void VincularAoPsicologo(Guid psicologoId)
	{
		PsicologoId = psicologoId;
	}

	public RegistroHumor RegistrarHumor(int nivel, string? textoOpcional = null, string? caminhoAudioOpcional = null)
	{
		RegistroHumor registro = new RegistroHumor(Id, nivel, textoOpcional, caminhoAudioOpcional);
		_registrosHumor.Add(registro);
		return registro;
	}

	public void Desativar()
	{
		Ativo = false;
	}

	public void AtualizarDados(string nome, string email, string? telefone, DateTime? dataNascimento, GeneroUsuario? genero = null)
	{
		if (string.IsNullOrWhiteSpace(nome))
		{
			throw new ArgumentException("Nome é obrigatório.", "nome");
		}
		if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
		{
			throw new ArgumentException("E-mail inválido.", "email");
		}
		Nome = nome.Trim();
		Email = email.Trim().ToLowerInvariant();
		Telefone = (string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim());
		DataNascimento = dataNascimento;
		Genero = genero;
	}

	public void AtualizarFoto(string? caminhoFoto)
	{
		FotoUrl = (string.IsNullOrWhiteSpace(caminhoFoto) ? null : caminhoFoto);
	}
}
