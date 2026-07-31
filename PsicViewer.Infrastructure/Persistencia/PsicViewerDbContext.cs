using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PsicViewer.Core.Entities;

namespace PsicViewer.Infrastructure.Persistencia;

public class PsicViewerDbContext : DbContext
{
	public DbSet<Paciente> Pacientes => Set<Paciente>();

	public DbSet<Psicologo> Psicologos => Set<Psicologo>();

	public DbSet<Mensagem> Mensagens => Set<Mensagem>();

	public DbSet<Vinculo> Vinculos => Set<Vinculo>();

	public DbSet<Questionario> Questionarios => Set<Questionario>();

	public DbSet<Pergunta> Perguntas => Set<Pergunta>();

	public DbSet<QuestionarioPaciente> QuestionarioPacientes => Set<QuestionarioPaciente>();

	public DbSet<Resposta> Respostas => Set<Resposta>();

	public PsicViewerDbContext(DbContextOptions<PsicViewerDbContext> options)
		: base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity(delegate(EntityTypeBuilder<Paciente> b)
		{
			b.ToTable("pacientes");
			b.HasKey((Paciente p) => p.Id);
			b.Property((Paciente p) => p.Nome).IsRequired().HasMaxLength(200);
			b.Property((Paciente p) => p.Email).IsRequired().HasMaxLength(200);
			b.HasIndex((Paciente p) => p.Email).IsUnique();
			b.Property((Paciente p) => p.SenhaHash).IsRequired();
			b.Property((Paciente p) => p.Telefone).HasMaxLength(30);
			b.Property((Paciente p) => p.FotoUrl).HasMaxLength(300);
			b.Ignore((Paciente p) => p.RegistrosHumor);
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Psicologo> b)
		{
			b.ToTable("psicologos");
			b.HasKey((Psicologo p) => p.Id);
			b.Property((Psicologo p) => p.Nome).IsRequired().HasMaxLength(200);
			b.Property((Psicologo p) => p.Email).IsRequired().HasMaxLength(200);
			b.HasIndex((Psicologo p) => p.Email).IsUnique();
			b.Property((Psicologo p) => p.SenhaHash).IsRequired();
			b.Property((Psicologo p) => p.Crp).IsRequired().HasMaxLength(30);
			b.HasIndex((Psicologo p) => p.Crp).IsUnique();
			b.Property((Psicologo p) => p.Telefone).HasMaxLength(30);
			b.Property((Psicologo p) => p.FotoUrl).HasMaxLength(300);
			b.Ignore((Psicologo p) => p.Pacientes);
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Mensagem> b)
		{
			b.ToTable("mensagens");
			b.HasKey((Mensagem m) => m.Id);
			b.Property((Mensagem m) => m.Conteudo).HasMaxLength(2000);
			b.Property((Mensagem m) => m.CaminhoArquivo).HasMaxLength(300);
			b.Property((Mensagem m) => m.NomeArquivoOriginal).HasMaxLength(300);
			b.HasIndex((Mensagem m) => new { m.RemetenteId, m.DestinatarioId });
			b.Property((Mensagem m) => m.CitacaoTextoPergunta).HasMaxLength(1000);
			b.Property((Mensagem m) => m.CitacaoTextoResposta).HasMaxLength(2000);
			b.Property((Mensagem m) => m.CitacaoQuestionarioTitulo).HasMaxLength(200);
			b.HasIndex((Mensagem m) => m.RespostaId);
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Vinculo> b)
		{
			b.ToTable("vinculos");
			b.HasKey((Vinculo v) => v.Id);
			b.HasIndex((Vinculo v) => new { v.PacienteId, v.PsicologoId });
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Questionario> b)
		{
			b.ToTable("questionarios");
			b.HasKey((Questionario q) => q.Id);
			b.Property((Questionario q) => q.Titulo).IsRequired().HasMaxLength(200);
			b.HasIndex((Questionario q) => q.PsicologoId);
			b.HasMany((Questionario q) => q.Perguntas).WithOne().HasForeignKey((Pergunta p) => p.QuestionarioId)
				.OnDelete(DeleteBehavior.Cascade);
			b.Navigation((Questionario q) => q.Perguntas).UsePropertyAccessMode(PropertyAccessMode.Field);
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Pergunta> b)
		{
			b.ToTable("perguntas");
			b.HasKey((Pergunta p) => p.Id);
			b.Property((Pergunta p) => p.TextoPergunta).IsRequired().HasMaxLength(1000);
			b.Property((Pergunta p) => p.Opcoes).HasMaxLength(1000);
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<QuestionarioPaciente> b)
		{
			b.ToTable("questionario_pacientes");
			b.HasKey((QuestionarioPaciente x) => x.Id);
			b.HasIndex((QuestionarioPaciente x) => new { x.QuestionarioId, x.PacienteId });
		});
		modelBuilder.Entity(delegate(EntityTypeBuilder<Resposta> b)
		{
			b.ToTable("respostas");
			b.HasKey((Resposta r) => r.Id);
			b.Property((Resposta r) => r.Data).HasColumnType("date");
			b.Property((Resposta r) => r.RespostaTexto).HasMaxLength(2000);
			b.Property((Resposta r) => r.Observacao).HasMaxLength(2000);
			b.Property((Resposta r) => r.AudioObservacao).HasMaxLength(300);
			b.HasIndex((Resposta r) => new { r.QuestionarioId, r.PacienteId });
			b.HasIndex((Resposta r) => r.PerguntaId);
			b.HasIndex((Resposta r) => new { r.PerguntaId, r.PacienteId, r.Data });
		});
	}
}
