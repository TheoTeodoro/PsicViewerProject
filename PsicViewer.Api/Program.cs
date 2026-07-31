using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using PsicViewer.Core.Entities;
using PsicViewer.Core.Interfaces;
using PsicViewer.Infrastructure.Persistencia;
using PsicViewer.Infrastructure.Repositories;
using PsicViewer.Api.Hubs;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

var connectionString = builder.Configuration.GetConnectionString("PsicViewerDb")
	?? throw new InvalidOperationException("Connection string 'PsicViewerDb' não configurada.");

builder.Services.AddDbContext<PsicViewerDbContext>(options =>
	options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
		mySqlOptions => mySqlOptions.EnableRetryOnFailure(maxRetryCount: 3)));

builder.Services.AddScoped<IPacienteRepository, PacienteRepositoryEfCore>();
builder.Services.AddScoped<IPsicologoRepository, PsicologoRepositoryEfCore>();
builder.Services.AddScoped<IMensagemRepository, MensagemRepositoryEfCore>();
builder.Services.AddScoped<IVinculoRepository, VinculoRepositoryEfCore>();
builder.Services.AddScoped<IQuestionarioRepository, QuestionarioRepositoryEfCore>();
builder.Services.AddScoped<IQuestionarioPacienteRepository, QuestionarioPacienteRepositoryEfCore>();
builder.Services.AddScoped<IRespostaRepository, RespostaRepositoryEfCore>();

builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
	{
		policy.AllowAnyHeader()
			  .AllowAnyMethod()
			  .SetIsOriginAllowed(_ => true)
			  .AllowCredentials();
	});
});

var app = builder.Build();

app.UseCors();

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<PsicViewerDbContext>();
	db.Database.EnsureCreated();
}

var pastaUploads = Path.Combine(app.Environment.ContentRootPath, "UploadedFiles");
Directory.CreateDirectory(pastaUploads);

app.UseStaticFiles(new StaticFileOptions
{
	FileProvider = new PhysicalFileProvider(pastaUploads),
	RequestPath = "/arquivos"
});

app.MapGet("/", () => "PsicViewer API rodando. Hub de chat em /chathub");

app.MapPost("/api/upload", async (HttpRequest request) =>
{
	if (!request.HasFormContentType)
		return Results.BadRequest("Content-Type precisa ser multipart/form-data.");

	var form = await request.ReadFormAsync();
	var arquivo = form.Files.GetFile("arquivo");

	if (arquivo is null || arquivo.Length == 0)
		return Results.BadRequest("Nenhum arquivo enviado.");

	var extensao = Path.GetExtension(arquivo.FileName);
	var nomeGerado = $"{Guid.NewGuid()}{extensao}";
	var caminhoFisico = Path.Combine(pastaUploads, nomeGerado);

	await using (var stream = File.Create(caminhoFisico))
	{
		await arquivo.CopyToAsync(stream);
	}

	return Results.Ok(new { caminho = $"/arquivos/{nomeGerado}", nomeOriginal = arquivo.FileName });
});

app.MapPost("/api/conta/cadastrar-paciente", async (CadastroPacienteRequest req, IPacienteRepository pacientes) =>
{
	try
	{
		if (await pacientes.ObterPorEmailAsync(req.Email) is not null)
			return Results.Conflict(new { erro = "Já existe uma conta com esse e-mail." });

		var paciente = new Paciente(req.Nome, req.Email, req.Senha);
		paciente.AtualizarDados(req.Nome, req.Email, req.Telefone, req.DataNascimento, ParseGenero(req.Genero));
		await pacientes.SalvarAsync(paciente);

		return Results.Ok(PacienteParaDto(paciente));
	}
	catch (ArgumentException ex)
	{
		return Results.BadRequest(new { erro = ex.Message });
	}
});

app.MapPost("/api/conta/cadastrar-psicologo", async (CadastroPsicologoRequest req, IPsicologoRepository psicologos) =>
{
	try
	{
		if (await psicologos.ObterPorEmailAsync(req.Email) is not null)
			return Results.Conflict(new { erro = "Já existe uma conta com esse e-mail." });

		if (await psicologos.ObterPorCrpAsync(req.Crp) is not null)
			return Results.Conflict(new { erro = "Já existe uma conta cadastrada com esse CRP." });

		var psicologo = new Psicologo(req.Nome, req.Email, req.Senha, req.Crp);
		psicologo.AtualizarDados(req.Nome, req.Email, req.Telefone, req.DataNascimento, ParseGenero(req.Genero), req.Crp);
		await psicologos.SalvarAsync(psicologo);

		return Results.Ok(PsicologoParaDto(psicologo));
	}
	catch (ArgumentException ex)
	{
		return Results.BadRequest(new { erro = ex.Message });
	}
});

app.MapPost("/api/conta/login", async (LoginRequest req, IPacienteRepository pacientes, IPsicologoRepository psicologos) =>
{
	var paciente = await pacientes.ObterPorEmailAsync(req.Email);
	if (paciente is not null && paciente.SenhaHash == req.Senha)
		return Results.Ok(PacienteParaDto(paciente));

	var psicologo = await psicologos.ObterPorEmailAsync(req.Email);
	if (psicologo is not null && psicologo.SenhaHash == req.Senha)
		return Results.Ok(PsicologoParaDto(psicologo));

	return Results.Json(new { erro = "E-mail ou senha inválidos." }, statusCode: 401);
});

app.MapGet("/api/conta/paciente/{id:guid}", async (Guid id, IPacienteRepository pacientes) =>
{
	var p = await pacientes.ObterPorIdAsync(id);
	return p is null ? Results.NotFound() : Results.Ok(PacienteParaDto(p));
});

app.MapGet("/api/conta/paciente/{id:guid}/perfil-publico", async (Guid id, IPacienteRepository pacientes) =>
{
	var p = await pacientes.ObterPorIdAsync(id);
	if (p is null) return Results.NotFound();

	int? idade = null;
	if (p.DataNascimento is DateTime nascimento)
	{
		idade = DateTime.UtcNow.Year - nascimento.Year;
		if (nascimento.Date > DateTime.UtcNow.AddYears(-idade.Value)) idade--;
	}

	return Results.Ok(new
	{
		id = p.Id,
		nome = p.Nome,
		fotoUrl = p.FotoUrl,
		idade,
		genero = p.Genero?.ToString(),
		email = p.Email,
		telefone = p.Telefone
	});
});

app.MapGet("/api/conta/psicologo/{id:guid}", async (Guid id, IPsicologoRepository psicologos) =>
{
	var p = await psicologos.ObterPorIdAsync(id);
	return p is null ? Results.NotFound() : Results.Ok(PsicologoParaDto(p));
});

app.MapPut("/api/conta/paciente/{id:guid}", async (Guid id, AtualizarPacienteRequest req, IPacienteRepository pacientes) =>
{
	var p = await pacientes.ObterPorIdAsync(id);
	if (p is null) return Results.NotFound();

	try
	{
		p.AtualizarDados(req.Nome, req.Email, req.Telefone, req.DataNascimento, ParseGenero(req.Genero));
		p.AtualizarFoto(req.FotoUrl);
		await pacientes.AtualizarAsync(p);
		return Results.Ok(PacienteParaDto(p));
	}
	catch (ArgumentException ex)
	{
		return Results.BadRequest(new { erro = ex.Message });
	}
});

app.MapPut("/api/conta/psicologo/{id:guid}", async (Guid id, AtualizarPsicologoRequest req, IPsicologoRepository psicologos) =>
{
	var p = await psicologos.ObterPorIdAsync(id);
	if (p is null) return Results.NotFound();

	try
	{
		p.AtualizarDados(req.Nome, req.Email, req.Telefone, req.DataNascimento, ParseGenero(req.Genero), req.Crp);
		p.AtualizarFoto(req.FotoUrl);
		await psicologos.AtualizarAsync(p);
		return Results.Ok(PsicologoParaDto(p));
	}
	catch (ArgumentException ex)
	{
		return Results.BadRequest(new { erro = ex.Message });
	}
});

app.MapGet("/api/conta/pacientes", async (IPacienteRepository pacientes) =>
{
	var lista = await pacientes.ListarTodosAsync();
	return Results.Ok(lista.Select(PacienteParaDto));
});

app.MapGet("/api/conta/psicologos", async (IPsicologoRepository psicologos) =>
{
	var lista = await psicologos.ListarTodosAsync();
	return Results.Ok(lista.Select(PsicologoParaDto));
});

app.MapPost("/api/vinculo/solicitar", async (SolicitarVinculoRequest req,
	IVinculoRepository vinculos, IPacienteRepository pacientes, IPsicologoRepository psicologos) =>
{
	if (await pacientes.ObterPorIdAsync(req.PacienteId) is null)
		return Results.NotFound(new { erro = "Paciente não encontrado." });

	if (await psicologos.ObterPorIdAsync(req.PsicologoId) is null)
		return Results.NotFound(new { erro = "Psicólogo não encontrado." });

	if (await vinculos.ObterVinculoAtivoDoPacienteAsync(req.PacienteId) is not null)
		return Results.Conflict(new { erro = "Você já tem um psicólogo vinculado ou uma solicitação pendente." });

	var vinculo = new Vinculo(req.PacienteId, req.PsicologoId, OrigemSolicitacao.Paciente);
	await vinculos.SalvarAsync(vinculo);

	return Results.Ok(VinculoParaDto(vinculo));
});

app.MapPost("/api/vinculo/psicologo-solicitar", async (SolicitarVinculoRequest req,
	IVinculoRepository vinculos, IPacienteRepository pacientes, IPsicologoRepository psicologos) =>
{
	if (await pacientes.ObterPorIdAsync(req.PacienteId) is null)
		return Results.NotFound(new { erro = "Paciente não encontrado." });

	if (await psicologos.ObterPorIdAsync(req.PsicologoId) is null)
		return Results.NotFound(new { erro = "Psicólogo não encontrado." });

	if (await vinculos.ObterVinculoAtivoDoPacienteAsync(req.PacienteId) is not null)
		return Results.Conflict(new { erro = "Esse paciente já tem um psicólogo vinculado ou uma solicitação pendente." });

	var vinculo = new Vinculo(req.PacienteId, req.PsicologoId, OrigemSolicitacao.Psicologo);
	await vinculos.SalvarAsync(vinculo);

	return Results.Ok(VinculoParaDto(vinculo));
});

app.MapPost("/api/vinculo/{id:guid}/aceitar", async (Guid id, IVinculoRepository vinculos) =>
{
	var vinculo = await vinculos.ObterPorIdAsync(id);
	if (vinculo is null) return Results.NotFound();

	try
	{
		vinculo.Aceitar();
		await vinculos.AtualizarAsync(vinculo);
		return Results.Ok(VinculoParaDto(vinculo));
	}
	catch (InvalidOperationException ex)
	{
		return Results.BadRequest(new { erro = ex.Message });
	}
});

app.MapPost("/api/vinculo/{id:guid}/recusar", async (Guid id, IVinculoRepository vinculos) =>
{
	var vinculo = await vinculos.ObterPorIdAsync(id);
	if (vinculo is null) return Results.NotFound();

	try
	{
		vinculo.Recusar();
		await vinculos.AtualizarAsync(vinculo);
		return Results.Ok(VinculoParaDto(vinculo));
	}
	catch (InvalidOperationException ex)
	{
		return Results.BadRequest(new { erro = ex.Message });
	}
});

app.MapPost("/api/vinculo/{id:guid}/encerrar", async (Guid id, IVinculoRepository vinculos) =>
{
	var vinculo = await vinculos.ObterPorIdAsync(id);
	if (vinculo is null) return Results.NotFound();

	try
	{
		vinculo.Encerrar();
		await vinculos.AtualizarAsync(vinculo);
		return Results.Ok(VinculoParaDto(vinculo));
	}
	catch (InvalidOperationException ex)
	{
		return Results.BadRequest(new { erro = ex.Message });
	}
});

app.MapPost("/api/vinculo/{id:guid}/marcar-pedido-visualizado", async (Guid id, IVinculoRepository vinculos) =>
{
	var vinculo = await vinculos.ObterPorIdAsync(id);
	if (vinculo is null) return Results.NotFound();

	vinculo.MarcarPedidoVisualizado();
	await vinculos.AtualizarAsync(vinculo);
	return Results.Ok();
});

app.MapPost("/api/vinculo/{id:guid}/marcar-aceito-visualizado", async (Guid id, IVinculoRepository vinculos) =>
{
	var vinculo = await vinculos.ObterPorIdAsync(id);
	if (vinculo is null) return Results.NotFound();

	vinculo.MarcarAceitoVisualizado();
	await vinculos.AtualizarAsync(vinculo);
	return Results.Ok();
});

app.MapGet("/api/vinculo/paciente/{pacienteId:guid}", async (Guid pacienteId,
	IVinculoRepository vinculos, IPsicologoRepository psicologos) =>
{
	var lista = await vinculos.ListarPorPacienteAsync(pacienteId);
	var resultado = new List<object>();

	foreach (var v in lista)
	{
		var psicologo = await psicologos.ObterPorIdAsync(v.PsicologoId);
		if (psicologo is not null)
			resultado.Add(VinculoComContatoParaDto(v, psicologo.Id, psicologo.Nome, psicologo.FotoUrl, psicologo.Crp));
	}

	return Results.Ok(resultado);
});

app.MapGet("/api/vinculo/psicologo/{psicologoId:guid}", async (Guid psicologoId,
	IVinculoRepository vinculos, IPacienteRepository pacientes) =>
{
	var lista = await vinculos.ListarPorPsicologoAsync(psicologoId);
	var resultado = new List<object>();

	foreach (var v in lista)
	{
		var paciente = await pacientes.ObterPorIdAsync(v.PacienteId);
		if (paciente is not null)
			resultado.Add(VinculoComContatoParaDto(v, paciente.Id, paciente.Nome, paciente.FotoUrl, null));
	}

	return Results.Ok(resultado);
});

app.MapPost("/api/questionario/criar", async (CriarQuestionarioRequest req,
	IQuestionarioRepository questionarios, IQuestionarioPacienteRepository vinculosQuestionario, IPsicologoRepository psicologos, IVinculoRepository vinculos) =>
{
	if (await psicologos.ObterPorIdAsync(req.PsicologoId) is null)
		return Results.NotFound(new { erro = "Psicólogo não encontrado." });

	try
	{
		var questionario = new Questionario(req.PsicologoId, req.Titulo);

		foreach (var p in req.Perguntas)
		{
			if (!Enum.TryParse<TipoPergunta>(p.Tipo, out var tipo))
				tipo = TipoPergunta.Texto;

			if (!TimeSpan.TryParse(p.Horario, out var horarioPergunta))
				horarioPergunta = new TimeSpan(8, 0, 0);

			var novaPergunta = questionario.AdicionarPergunta(tipo, p.Texto, horarioPergunta, p.Opcoes);
			if (!p.Ativa)
				questionario.DesativarPergunta(novaPergunta.Id);
		}

		await questionarios.SalvarAsync(questionario);

		var vinculosCriados = 0;
		foreach (var p in req.Pacientes)
		{
			var vinculoPaciente = await vinculos.ObterVinculoAtivoAsync(p.PacienteId, req.PsicologoId);
			if (vinculoPaciente is null || vinculoPaciente.Status != StatusVinculo.Aceito)
				continue;

			var vinculo = new QuestionarioPaciente(questionario.Id, p.PacienteId, p.DiasSemana);
			await vinculosQuestionario.SalvarAsync(vinculo);
			vinculosCriados++;
		}

		return Results.Ok(QuestionarioParaDto(questionario, vinculosCriados));
	}
	catch (ArgumentException ex)
	{
		return Results.BadRequest(new { erro = ex.Message });
	}
	catch (Exception ex)
	{
		return Results.Json(new { erro = $"{ex.GetType().Name}: {ex.Message}" }, statusCode: 500);
	}
});

app.MapGet("/api/questionario/psicologo/{psicologoId:guid}", async (Guid psicologoId,
	IQuestionarioRepository questionarios, IQuestionarioPacienteRepository vinculosQuestionario) =>
{
	var lista = await questionarios.ListarPorPsicologoAsync(psicologoId);
	var resultado = new List<object>();

	foreach (var q in lista)
	{
		var vinculos = await vinculosQuestionario.ListarPorQuestionarioAsync(q.Id);
		resultado.Add(QuestionarioParaDto(q, vinculos.Count));
	}

	return Results.Ok(resultado);
});

app.MapGet("/api/questionario/{id:guid}/editar", async (Guid id,
	IQuestionarioRepository questionarios, IQuestionarioPacienteRepository vinculosQuestionario, IPacienteRepository pacientes) =>
{
	var questionario = await questionarios.ObterPorIdAsync(id);
	if (questionario is null) return Results.NotFound();

	var vinculos = await vinculosQuestionario.ListarPorQuestionarioAsync(id);
	var pacientesInfo = new List<object>();

	foreach (var v in vinculos)
	{
		var paciente = await pacientes.ObterPorIdAsync(v.PacienteId);
		if (paciente is not null)
			pacientesInfo.Add(new { pacienteId = paciente.Id, nome = paciente.Nome, diasSemana = v.DiasSemana });
	}

	return Results.Ok(new
	{
		id = questionario.Id,
		titulo = questionario.Titulo,
		status = questionario.Status.ToString(),
		perguntas = questionario.Perguntas.OrderBy(p => p.Ordem).Select(p => new
		{
			id = p.Id,
			tipo = p.Tipo.ToString(),
			texto = p.TextoPergunta,
			opcoes = p.Opcoes,
			horario = p.HorarioNotificacao.ToString(@"hh\:mm"),
			ativa = p.Ativa
		}),
		pacientes = pacientesInfo
	});
});

app.MapPut("/api/questionario/{id:guid}", async (Guid id, EditarQuestionarioRequest req,
	IQuestionarioRepository questionarios, IQuestionarioPacienteRepository vinculosQuestionario, IVinculoRepository vinculos,
	IRespostaRepository respostasRepo, PsicViewerDbContext db) =>
{
	var questionario = await questionarios.ObterPorIdAsync(id);
	if (questionario is null) return Results.NotFound();

	var strategy = db.Database.CreateExecutionStrategy();

	try
	{
		return await strategy.ExecuteAsync(async () =>
		{
			await using var transaction = await db.Database.BeginTransactionAsync();

			questionario.AtualizarTitulo(req.Titulo);

			foreach (var p in req.Perguntas)
			{
				if (!TimeSpan.TryParse(p.Horario, out var horario))
					horario = new TimeSpan(8, 0, 0);

				if (p.Id is Guid perguntaId && questionario.Perguntas.Any(x => x.Id == perguntaId))
				{
					questionario.AtualizarHorarioPergunta(perguntaId, horario);
					if (p.Ativa) questionario.AtivarPergunta(perguntaId);
					else questionario.DesativarPergunta(perguntaId);
				}
				else
				{
					if (!Enum.TryParse<TipoPergunta>(p.Tipo, out var tipo))
						tipo = TipoPergunta.Texto;

					var nova = questionario.AdicionarPergunta(tipo, p.Texto, horario, p.Opcoes);

					db.Perguntas.Add(nova);

					if (!p.Ativa)
						questionario.DesativarPergunta(nova.Id);
				}
			}

			var idsRecebidos = req.Perguntas.Where(p => p.Id.HasValue).Select(p => p.Id!.Value).ToHashSet();
			var perguntasRemovidas = questionario.Perguntas.Where(p => !idsRecebidos.Contains(p.Id)).ToList();

			if (perguntasRemovidas.Count > 0)
			{
				var respostasDoQuestionario = await respostasRepo.ListarPorQuestionarioAsync(id);

				foreach (var perguntaRemovida in perguntasRemovidas)
				{
					if (respostasDoQuestionario.Any(r => r.PerguntaId == perguntaRemovida.Id))
					{
						return Results.BadRequest(new
						{
							erro = $"A pergunta \"{perguntaRemovida.TextoPergunta}\" já tem respostas registradas e não pode ser apagada — desative-a em vez disso."
						});
					}

					questionario.RemoverPergunta(perguntaRemovida.Id);
				}
			}

			await questionarios.AtualizarAsync(questionario);

			var vinculosAtuais = await vinculosQuestionario.ListarPorQuestionarioAsync(id);
			foreach (var v in vinculosAtuais)
				await vinculosQuestionario.RemoverAsync(id, v.PacienteId);

			foreach (var p in req.Pacientes)
			{
				var vinculoPaciente = await vinculos.ObterVinculoAtivoAsync(p.PacienteId, questionario.PsicologoId);
				if (vinculoPaciente is null || vinculoPaciente.Status != StatusVinculo.Aceito)
					continue;

				await vinculosQuestionario.SalvarAsync(new QuestionarioPaciente(id, p.PacienteId, p.DiasSemana));
			}

			var vinculosFinais = await vinculosQuestionario.ListarPorQuestionarioAsync(id);

			await transaction.CommitAsync();
			return Results.Ok(QuestionarioParaDto(questionario, vinculosFinais.Count));
		});
	}
	catch (ArgumentException ex)
	{
		return Results.BadRequest(new { erro = ex.Message });
	}
	catch (DbUpdateConcurrencyException ex)
	{
		var detalhes = ex.Entries.Select(e =>
		{
			var chave = e.Metadata.FindPrimaryKey()?.Properties
				.Select(p => $"{p.Name}={e.Property(p.Name).CurrentValue}");
			return $"{e.Entity.GetType().Name}[{string.Join(",", chave ?? Enumerable.Empty<string>())}] (Estado={e.State})";
		});

		return Results.Json(new { erro = $"DbUpdateConcurrencyException envolvendo: {string.Join(" | ", detalhes)}" }, statusCode: 500);
	}
	catch (Exception ex)
	{
		return Results.Json(new { erro = $"{ex.GetType().Name}: {ex.Message}" }, statusCode: 500);
	}
});

app.MapDelete("/api/questionario/{id:guid}", async (Guid id,
	IQuestionarioRepository questionarios, IQuestionarioPacienteRepository vinculosQuestionario,
	IRespostaRepository respostasRepo, PsicViewerDbContext db) =>
{
	var questionario = await questionarios.ObterPorIdAsync(id);
	if (questionario is null) return Results.NotFound();

	try
	{
		var vinculos = await vinculosQuestionario.ListarPorQuestionarioAsync(id);
		foreach (var v in vinculos)
			await vinculosQuestionario.RemoverAsync(id, v.PacienteId);

		var respostas = await respostasRepo.ListarPorQuestionarioAsync(id);
		if (respostas.Count > 0)
			db.Respostas.RemoveRange(respostas);

		db.Questionarios.Remove(questionario);
		await db.SaveChangesAsync();

		return Results.Ok();
	}
	catch (Exception ex)
	{
		return Results.Json(new { erro = $"{ex.GetType().Name}: {ex.Message}" }, statusCode: 500);
	}
});

app.MapGet("/api/questionario/psicologo/{psicologoId:guid}/resumo", async (Guid psicologoId,
	IQuestionarioRepository questionarios, IQuestionarioPacienteRepository vinculosQuestionario) =>
{
	var meusQuestionarios = await questionarios.ListarPorPsicologoAsync(psicologoId);
	var ativos = meusQuestionarios.Where(q => q.Status == StatusQuestionario.Ativo).ToList();

	var emUso = 0;
	foreach (var q in ativos)
	{
		var vinculos = await vinculosQuestionario.ListarPorQuestionarioAsync(q.Id);
		if (vinculos.Count > 0) emUso++;
	}

	return Results.Ok(new { questionariosAtivosEmUso = emUso });
});

app.MapGet("/api/questionario/psicologo/{psicologoId:guid}/pendentes-hoje", async (Guid psicologoId,
	IQuestionarioRepository questionarios, IQuestionarioPacienteRepository vinculosQuestionario, IRespostaRepository respostasRepo) =>
{
	var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
	var abreviacaoHoje = AbreviacaoDiaSemana(hoje);

	var meusQuestionarios = await questionarios.ListarPorPsicologoAsync(psicologoId);
	var pendentes = 0;

	foreach (var q in meusQuestionarios.Where(q => q.Status == StatusQuestionario.Ativo))
	{
		var perguntasAtivas = q.Perguntas.Where(p => p.Ativa).ToList();
		if (perguntasAtivas.Count == 0) continue;

		var vinculos = await vinculosQuestionario.ListarPorQuestionarioAsync(q.Id);

		foreach (var v in vinculos)
		{
			var dias = (v.DiasSemana ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries);
			if (dias.Length > 0 && !dias.Contains(abreviacaoHoje)) continue;

			foreach (var p in perguntasAtivas)
			{
				if (await respostasRepo.ObterPorPerguntaPacienteEDataAsync(p.Id, v.PacienteId, hoje) is null)
					pendentes++;
			}
		}
	}

	return Results.Ok(new { pendentes });
});

app.MapGet("/api/questionario/paciente/{pacienteId:guid}", async (Guid pacienteId,
	IQuestionarioPacienteRepository vinculosQuestionario, IQuestionarioRepository questionarios, IRespostaRepository respostasRepo) =>
{
	var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
	var abreviacaoHoje = AbreviacaoDiaSemana(hoje);

	var meusVinculos = await vinculosQuestionario.ListarPorPacienteAsync(pacienteId);
	var resultado = new List<object>();

	foreach (var v in meusVinculos)
	{
		var questionario = await questionarios.ObterPorIdAsync(v.QuestionarioId);
		if (questionario is null || questionario.Status != StatusQuestionario.Ativo) continue;

		var dias = (v.DiasSemana ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries);
		if (dias.Length > 0 && !dias.Contains(abreviacaoHoje)) continue;

		var perguntasAtivas = questionario.Perguntas.Where(p => p.Ativa).ToList();

		var respondidasHoje = 0;
		foreach (var p in perguntasAtivas)
		{
			if (await respostasRepo.ObterPorPerguntaPacienteEDataAsync(p.Id, pacienteId, hoje) is not null)
				respondidasHoje++;
		}

		resultado.Add(new
		{
			id = questionario.Id,
			titulo = questionario.Titulo,
			diasSemana = v.DiasSemana,
			quantidadePerguntas = perguntasAtivas.Count,
			quantidadeRespondidasHoje = respondidasHoje
		});
	}

	return Results.Ok(resultado);
});

app.MapGet("/api/questionario/paciente/{pacienteId:guid}/proxima-pergunta", async (Guid pacienteId,
	IQuestionarioPacienteRepository vinculosQuestionario, IQuestionarioRepository questionarios, IRespostaRepository respostasRepo) =>
{
	var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
	var abreviacaoHoje = AbreviacaoDiaSemana(hoje);
	var agora = DateTime.Now.TimeOfDay;

	var meusVinculos = await vinculosQuestionario.ListarPorPacienteAsync(pacienteId);
	var candidatos = new List<Questionario>();

	foreach (var v in meusVinculos)
	{
		var questionario = await questionarios.ObterPorIdAsync(v.QuestionarioId);
		if (questionario is null || questionario.Status != StatusQuestionario.Ativo) continue;

		var dias = (v.DiasSemana ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries);
		if (dias.Length > 0 && !dias.Contains(abreviacaoHoje)) continue;

		candidatos.Add(questionario);
	}

	foreach (var questionario in candidatos.OrderBy(q => q.CriadoEm))
	{
		Pergunta? candidata = null;
		var menorDiferenca = TimeSpan.MaxValue;

		foreach (var p in questionario.Perguntas.Where(p => p.Ativa))
		{
			if (await respostasRepo.ObterPorPerguntaPacienteEDataAsync(p.Id, pacienteId, hoje) is not null)
				continue;

			var diferenca = agora > p.HorarioNotificacao ? agora - p.HorarioNotificacao : p.HorarioNotificacao - agora;
			if (diferenca < menorDiferenca)
			{
				menorDiferenca = diferenca;
				candidata = p;
			}
		}

		if (candidata is not null)
		{
			return Results.Ok(new
			{
				temPergunta = true,
				questionarioId = questionario.Id,
				questionarioTitulo = questionario.Titulo,
				perguntaId = candidata.Id,
				perguntaTexto = candidata.TextoPergunta,
				tipo = candidata.Tipo.ToString(),
				opcoes = candidata.Opcoes,
				horario = candidata.HorarioNotificacao.ToString(@"hh\:mm")
			});
		}
	}

	return Results.Ok(new { temPergunta = false });
});

app.MapGet("/api/questionario/{id:guid}/responder/{pacienteId:guid}", async (Guid id, Guid pacienteId,
	IQuestionarioRepository questionarios, IQuestionarioPacienteRepository vinculosQuestionario, IRespostaRepository respostasRepo) =>
{
	var questionario = await questionarios.ObterPorIdAsync(id);
	if (questionario is null) return Results.NotFound();

	var vinculos = await vinculosQuestionario.ListarPorQuestionarioAsync(id);
	if (!vinculos.Any(v => v.PacienteId == pacienteId))
		return Results.Forbid();

	var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
	var perguntasDto = new List<object>();

	foreach (var p in questionario.Perguntas.Where(p => p.Ativa).OrderBy(p => p.Ordem))
	{
		var respostaHoje = await respostasRepo.ObterPorPerguntaPacienteEDataAsync(p.Id, pacienteId, hoje);

		perguntasDto.Add(new
		{
			id = p.Id,
			tipo = p.Tipo.ToString(),
			texto = p.TextoPergunta,
			opcoes = p.Opcoes,
			respondidaHoje = respostaHoje is not null,
			valorEscala = respostaHoje?.ValorEscala,
			respostaTexto = respostaHoje?.RespostaTexto,
			observacao = respostaHoje?.Observacao,
			audioObservacao = respostaHoje?.AudioObservacao
		});
	}

	return Results.Ok(new
	{
		id = questionario.Id,
		titulo = questionario.Titulo,
		perguntas = perguntasDto
	});
});

app.MapPost("/api/questionario/pergunta/{perguntaId:guid}/responder", async (Guid perguntaId, ResponderPerguntaRequest req,
	IRespostaRepository respostas) =>
{
	try
	{
		var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
		var existente = await respostas.ObterPorPerguntaPacienteEDataAsync(perguntaId, req.PacienteId, hoje);

		if (existente is not null)
		{
			existente.AtualizarResposta(req.ValorEscala, req.RespostaTexto, req.Observacao, req.AudioObservacao);
			await respostas.AtualizarAsync(existente);
			return Results.Ok(new { id = existente.Id });
		}

		var nova = new Resposta(perguntaId, req.QuestionarioId, req.PacienteId, req.ValorEscala, req.RespostaTexto, req.Observacao, req.AudioObservacao);
		await respostas.SalvarAsync(nova);
		return Results.Ok(new { id = nova.Id });
	}
	catch (ArgumentException ex)
	{
		return Results.BadRequest(new { erro = ex.Message });
	}
});

app.MapGet("/api/questionario/paciente/{pacienteId:guid}/historico", async (Guid pacienteId,
	IRespostaRepository respostasRepo, IQuestionarioRepository questionarios) =>
{
	var historico = await respostasRepo.ListarHistoricoPorPacienteAsync(pacienteId);
	var resultado = new List<object>();

	foreach (var r in historico)
	{
		var questionario = await questionarios.ObterPorIdAsync(r.QuestionarioId);
		var pergunta = questionario?.Perguntas.FirstOrDefault(p => p.Id == r.PerguntaId);

		resultado.Add(new
		{
			respostaId = r.Id,
			questionarioId = r.QuestionarioId,
			data = r.Data.ToString("yyyy-MM-dd"),
			questionarioTitulo = questionario?.Titulo ?? string.Empty,
			perguntaTexto = pergunta?.TextoPergunta ?? string.Empty,
			tipoPergunta = pergunta?.Tipo.ToString() ?? "Texto",
			valorEscala = r.ValorEscala,
			respostaTexto = r.RespostaTexto,
			observacao = r.Observacao,
			audioObservacao = r.AudioObservacao,
			respondidoEm = r.RespondidoEm.ToString("O")
		});
	}

	return Results.Ok(resultado);
});

app.MapGet("/api/questionario/{id:guid}/paciente/{pacienteId:guid}/dia/{data}", async (
	Guid id, Guid pacienteId, string data,
	IQuestionarioRepository questionarios, IRespostaRepository respostasRepo) =>
{
	if (!DateOnly.TryParse(data, out var dataAlvo))
		return Results.BadRequest(new { erro = "Data inválida." });

	var questionario = await questionarios.ObterPorIdAsync(id);
	if (questionario is null) return Results.NotFound();

	var perguntasDto = new List<object>();

	foreach (var p in questionario.Perguntas.OrderBy(p => p.Ordem))
	{
		var resposta = await respostasRepo.ObterPorPerguntaPacienteEDataAsync(p.Id, pacienteId, dataAlvo);

		perguntasDto.Add(new
		{
			id = p.Id,
			tipo = p.Tipo.ToString(),
			texto = p.TextoPergunta,
			opcoes = p.Opcoes,
			respondida = resposta is not null,
			valorEscala = resposta?.ValorEscala,
			respostaTexto = resposta?.RespostaTexto,
			observacao = resposta?.Observacao,
			audioObservacao = resposta?.AudioObservacao
		});
	}

	return Results.Ok(new
	{
		id = questionario.Id,
		titulo = questionario.Titulo,
		data = dataAlvo.ToString("yyyy-MM-dd"),
		perguntas = perguntasDto
	});
});

app.MapGet("/api/questionario/psicologo/{psicologoId:guid}/paciente/{pacienteId:guid}/historico", async (
	Guid psicologoId, Guid pacienteId,
	IQuestionarioRepository questionarios, IRespostaRepository respostasRepo) =>
{
	var meusQuestionarios = await questionarios.ListarPorPsicologoAsync(psicologoId);
	var itens = new List<(DateTime respondidoEm, object payload)>();

	foreach (var q in meusQuestionarios)
	{
		var respostas = await respostasRepo.ListarPorQuestionarioEPacienteAsync(q.Id, pacienteId);

		foreach (var r in respostas)
		{
			var pergunta = q.Perguntas.FirstOrDefault(p => p.Id == r.PerguntaId);

			itens.Add((r.RespondidoEm, new
			{
				respostaId = r.Id,
				questionarioId = r.QuestionarioId,
				data = r.Data.ToString("yyyy-MM-dd"),
				questionarioTitulo = q.Titulo,
				perguntaTexto = pergunta?.TextoPergunta ?? string.Empty,
				tipoPergunta = pergunta?.Tipo.ToString() ?? "Texto",
				valorEscala = r.ValorEscala,
				respostaTexto = r.RespostaTexto,
				observacao = r.Observacao,
				audioObservacao = r.AudioObservacao,
				respondidoEm = r.RespondidoEm.ToString("O")
			}));
		}
	}

	var ordenado = itens.OrderByDescending(i => i.respondidoEm).Select(i => i.payload);
	return Results.Ok(ordenado);
});

app.MapGet("/api/questionario/psicologo/{psicologoId:guid}/paciente/{pacienteId:guid}/relatorio-humor", async (
	Guid psicologoId, Guid pacienteId, DateTime? inicio, DateTime? fim,
	IQuestionarioRepository questionarios, IRespostaRepository respostasRepo) =>
{
	var meusQuestionarios = await questionarios.ListarPorPsicologoAsync(psicologoId);
	var pontos = new List<(DateOnly Data, int Valor)>();

	DateOnly? inicioAlvo = inicio.HasValue ? DateOnly.FromDateTime(inicio.Value) : null;
	DateOnly? fimAlvo = fim.HasValue ? DateOnly.FromDateTime(fim.Value) : null;

	foreach (var q in meusQuestionarios)
	{
		var respostas = await respostasRepo.ListarPorQuestionarioEPacienteAsync(q.Id, pacienteId);

		foreach (var r in respostas)
		{
			if (r.ValorEscala is not int valor) continue;
			if (inicioAlvo.HasValue && r.Data < inicioAlvo.Value) continue;
			if (fimAlvo.HasValue && r.Data > fimAlvo.Value) continue;

			pontos.Add((r.Data, valor));
		}
	}

	var agrupado = pontos
		.GroupBy(p => p.Data)
		.OrderBy(g => g.Key)
		.Select(g => new
		{
			data = g.Key.ToString("yyyy-MM-dd"),
			mediaHumor = Math.Round(g.Average(p => p.Valor), 2)
		});

	return Results.Ok(agrupado);
});

app.MapGet("/api/questionario/psicologo/{psicologoId:guid}/paciente/{pacienteId:guid}/pergunta/{perguntaId:guid}/serie", async (
	Guid psicologoId, Guid pacienteId, Guid perguntaId, DateTime? inicio, DateTime? fim,
	IQuestionarioRepository questionarios, IRespostaRepository respostasRepo) =>
{
	var meusQuestionarios = await questionarios.ListarPorPsicologoAsync(psicologoId);
	var questionarioDaPergunta = meusQuestionarios.FirstOrDefault(q => q.Perguntas.Any(p => p.Id == perguntaId));
	if (questionarioDaPergunta is null) return Results.NotFound();

	var respostas = await respostasRepo.ListarPorQuestionarioEPacienteAsync(questionarioDaPergunta.Id, pacienteId);

	DateOnly? inicioAlvo = inicio.HasValue ? DateOnly.FromDateTime(inicio.Value) : null;
	DateOnly? fimAlvo = fim.HasValue ? DateOnly.FromDateTime(fim.Value) : null;

	var pontos = respostas
		.Where(r => r.PerguntaId == perguntaId)
		.Where(r => (!inicioAlvo.HasValue || r.Data >= inicioAlvo.Value) && (!fimAlvo.HasValue || r.Data <= fimAlvo.Value))
		.OrderBy(r => r.Data)
		.Select(r => new
		{
			data = r.Data.ToString("yyyy-MM-dd"),
			valorEscala = r.ValorEscala,
			respostaTexto = r.RespostaTexto,
			observacao = r.Observacao,
			audioObservacao = r.AudioObservacao
		});

	return Results.Ok(pontos);
});

app.MapGet("/api/questionario/psicologo/{psicologoId:guid}/respostas-nao-vistas", async (Guid psicologoId,
	IQuestionarioRepository questionarios, IRespostaRepository respostasRepo, IPacienteRepository pacientes) =>
{
	var meusQuestionarios = await questionarios.ListarPorPsicologoAsync(psicologoId);
	var resultado = new List<object>();

	foreach (var q in meusQuestionarios)
	{
		var respostas = await respostasRepo.ListarPorQuestionarioAsync(q.Id);

		foreach (var r in respostas.Where(r => !r.TemFeedback))
		{
			var paciente = await pacientes.ObterPorIdAsync(r.PacienteId);
			var pergunta = q.Perguntas.FirstOrDefault(p => p.Id == r.PerguntaId);

			resultado.Add(new
			{
				respostaId = r.Id,
				pacienteId = r.PacienteId,
				pacienteNome = paciente?.Nome ?? "Alguém",
				questionarioTitulo = q.Titulo,
				perguntaTexto = pergunta?.TextoPergunta ?? string.Empty,
				respondidoEm = r.RespondidoEm.ToString("O")
			});
		}
	}

	return Results.Ok(resultado);
});

app.MapPost("/api/resposta/{id:guid}/marcar-visualizada", async (Guid id, IRespostaRepository respostas) =>
{
	var resposta = await respostas.ObterPorIdAsync(id);
	if (resposta is null) return Results.NotFound();

	resposta.MarcarVisualizada();
	await respostas.AtualizarAsync(resposta);
	return Results.Ok();
});

app.MapGet("/api/resposta/{id:guid}/detalhe", async (Guid id,
	IRespostaRepository respostas, IQuestionarioRepository questionarios, IPacienteRepository pacientes) =>
{
	var resposta = await respostas.ObterPorIdAsync(id);
	if (resposta is null) return Results.NotFound();

	var questionario = await questionarios.ObterPorIdAsync(resposta.QuestionarioId);
	var pergunta = questionario?.Perguntas.FirstOrDefault(p => p.Id == resposta.PerguntaId);
	var paciente = await pacientes.ObterPorIdAsync(resposta.PacienteId);

	return Results.Ok(new
	{
		respostaId = resposta.Id,
		pacienteId = resposta.PacienteId,
		pacienteNome = paciente?.Nome ?? "Alguém",
		questionarioTitulo = questionario?.Titulo ?? string.Empty,
		perguntaTexto = pergunta?.TextoPergunta ?? string.Empty,
		valorEscala = resposta.ValorEscala,
		respostaTexto = resposta.RespostaTexto,
		observacao = resposta.Observacao,
		audioObservacao = resposta.AudioObservacao,
		respondidoEm = resposta.RespondidoEm.ToString("O")
	});
});

app.MapGet("/api/mensagens/nao-lidas/{usuarioId:guid}", async (Guid usuarioId,
	IMensagemRepository mensagens, IPacienteRepository pacientes, IPsicologoRepository psicologos) =>
{
	var naoLidas = await mensagens.ObterNaoLidasAsync(usuarioId);

	var resultado = new List<object>();
	foreach (var m in naoLidas.OrderByDescending(m => m.EnviadaEm))
	{
		string nomeRemetente = "Alguém";

		var paciente = await pacientes.ObterPorIdAsync(m.RemetenteId);
		if (paciente is not null)
		{
			nomeRemetente = paciente.Nome;
		}
		else
		{
			var psicologo = await psicologos.ObterPorIdAsync(m.RemetenteId);
			if (psicologo is not null) nomeRemetente = psicologo.Nome;
		}

		resultado.Add(new
		{
			mensagemId = m.Id,
			remetenteId = m.RemetenteId,
			remetenteNome = nomeRemetente,
			tipoConteudo = m.TipoConteudo.ToString(),
			nomeArquivoOriginal = m.NomeArquivoOriginal,
			enviadaEm = m.EnviadaEm.ToString("O"),
			ehFeedback = m.RespostaId.HasValue
		});
	}

	return Results.Ok(resultado);
});

app.MapHub<ChatHub>("/chathub");

app.MapPost("/api/dev/seed-teste", async (
	IPacienteRepository pacientes, IPsicologoRepository psicologos, IVinculoRepository vinculos,
	IQuestionarioRepository questionarios, IQuestionarioPacienteRepository vinculosQuestionario,
	PsicViewerDbContext db) =>
{
	if (await pacientes.ObterPorEmailAsync("maria.santos@teste.com") is not null)
		return Results.Conflict(new { erro = "Seed já foi executado antes (maria.santos@teste.com já existe). Apague as tabelas se quiser rodar de novo." });

	var paciente = new Paciente("Maria Santos", "maria.santos@teste.com", "123456");
	paciente.AtualizarDados("Maria Santos", "maria.santos@teste.com", "11999990000", new DateTime(1995, 4, 12), null);
	await pacientes.SalvarAsync(paciente);

	var psicologo = new Psicologo("Dr. João Oliveira", "joao.oliveira@teste.com", "123456", "CRP-12345");
	psicologo.AtualizarDados("Dr. João Oliveira", "joao.oliveira@teste.com", "11988880000", new DateTime(1985, 6, 20), null, "CRP-12345");
	await psicologos.SalvarAsync(psicologo);

	var vinculo = new Vinculo(paciente.Id, psicologo.Id, OrigemSolicitacao.Paciente);
	vinculo.Aceitar();
	await vinculos.SalvarAsync(vinculo);

	var questionario = new Questionario(psicologo.Id, "Questionário Diário de Humor");
	var pEscala = questionario.AdicionarPergunta(TipoPergunta.Escala, "Como você está se sentindo hoje?", new TimeSpan(8, 0, 0));
	var pTexto = questionario.AdicionarPergunta(TipoPergunta.Texto, "O que aconteceu de mais marcante hoje?", new TimeSpan(20, 0, 0));
	var pEscolha = questionario.AdicionarPergunta(TipoPergunta.MultiplaEscolha, "Como está seu sono?", new TimeSpan(8, 0, 0), "Ótimo|Bom|Regular|Ruim|Péssimo");
	await questionarios.SalvarAsync(questionario);

	await vinculosQuestionario.SalvarAsync(new QuestionarioPaciente(questionario.Id, paciente.Id));

	var diasSeed = new[]
	{
		(Data: new DateOnly(2026, 7, 24), Escala: 3, Texto: "Tive uma reunião de trabalho estressante.", Opcao: "Regular", Observacao: (string?)null),
		(Data: new DateOnly(2026, 7, 25), Escala: 2, Texto: "Fiquei em casa descansando, mas me senti sozinha.", Opcao: "Ruim", Observacao: "Foi um dia mais difícil emocionalmente, mas passou."),
		(Data: new DateOnly(2026, 7, 26), Escala: 4, Texto: "Saí com amigos e me diverti bastante.", Opcao: "Bom", Observacao: (string?)null),
	};

	foreach (var dia in diasSeed)
	{
		var respondidoEm = dia.Data.ToDateTime(new TimeOnly(8, 30));

		var rEscala = new Resposta(pEscala.Id, questionario.Id, paciente.Id, dia.Escala, null, dia.Observacao);
		ForcarDataResposta(rEscala, dia.Data, respondidoEm);
		db.Respostas.Add(rEscala);

		var rTexto = new Resposta(pTexto.Id, questionario.Id, paciente.Id, null, dia.Texto);
		ForcarDataResposta(rTexto, dia.Data, respondidoEm.AddHours(12));
		db.Respostas.Add(rTexto);

		var rEscolha = new Resposta(pEscolha.Id, questionario.Id, paciente.Id, null, dia.Opcao);
		ForcarDataResposta(rEscolha, dia.Data, respondidoEm);
		db.Respostas.Add(rEscolha);
	}

	await db.SaveChangesAsync();

	return Results.Ok(new
	{
		pacienteId = paciente.Id,
		pacienteEmail = paciente.Email,
		psicologoId = psicologo.Id,
		psicologoEmail = psicologo.Email,
		senha = "123456",
		questionarioId = questionario.Id,
		mensagem = "Seed criado: 1 paciente, 1 psicólogo, vínculo aceito, 1 questionário (3 perguntas) e respostas de 24, 25 e 26/07/2026."
	});
});

app.Run();

static object PacienteParaDto(Paciente p) => new
{
	id = p.Id,
	nome = p.Nome,
	email = p.Email,
	telefone = p.Telefone,
	dataNascimento = p.DataNascimento,
	genero = p.Genero?.ToString(),
	fotoUrl = p.FotoUrl,
	tipo = "Paciente"
};

static object PsicologoParaDto(Psicologo p) => new
{
	id = p.Id,
	nome = p.Nome,
	email = p.Email,
	crp = p.Crp,
	telefone = p.Telefone,
	dataNascimento = p.DataNascimento,
	genero = p.Genero?.ToString(),
	fotoUrl = p.FotoUrl,
	tipo = "Psicologo"
};

static GeneroUsuario? ParseGenero(string? valor)
	=> Enum.TryParse<GeneroUsuario>(valor, out var g) ? g : null;

static void ForcarDataResposta(Resposta resposta, DateOnly data, DateTime respondidoEm)
{
	typeof(Resposta).GetProperty(nameof(Resposta.Data))!.SetValue(resposta, data);
	typeof(Resposta).GetProperty(nameof(Resposta.RespondidoEm))!.SetValue(resposta, respondidoEm);
}

static string AbreviacaoDiaSemana(DateOnly data) => data.DayOfWeek switch
{
	DayOfWeek.Sunday => "Dom",
	DayOfWeek.Monday => "Seg",
	DayOfWeek.Tuesday => "Ter",
	DayOfWeek.Wednesday => "Qua",
	DayOfWeek.Thursday => "Qui",
	DayOfWeek.Friday => "Sex",
	DayOfWeek.Saturday => "Sab",
	_ => "Dom"
};

static object VinculoParaDto(Vinculo v) => new
{
	id = v.Id,
	pacienteId = v.PacienteId,
	psicologoId = v.PsicologoId,
	status = v.Status.ToString(),
	origem = v.Origem.ToString(),
	solicitadoEm = v.SolicitadoEm,
	respondidoEm = v.RespondidoEm,
	pedidoVisualizado = v.PedidoVisualizado,
	aceitoVisualizado = v.AceitoVisualizado
};

static object VinculoComContatoParaDto(Vinculo v, Guid contatoId, string contatoNome, string? contatoFotoUrl, string? contatoCrp) => new
{
	id = v.Id,
	status = v.Status.ToString(),
	origem = v.Origem.ToString(),
	solicitadoEm = v.SolicitadoEm,
	respondidoEm = v.RespondidoEm,
	pedidoVisualizado = v.PedidoVisualizado,
	aceitoVisualizado = v.AceitoVisualizado,
	contatoId,
	contatoNome,
	contatoFotoUrl,
	contatoCrp
};

static object QuestionarioParaDto(Questionario q, int quantidadePacientesVinculados) => new
{
	id = q.Id,
	titulo = q.Titulo,
	status = quantidadePacientesVinculados > 0 ? "Ativo" : "Inativo",
	criadoEm = q.CriadoEm,
	quantidadePerguntas = q.Perguntas.Count(p => p.Ativa),
	quantidadePacientesVinculados
};

record PerguntaRequest(Guid? Id, string Tipo, string Texto, string? Opcoes, string Horario, bool Ativa);
record PacienteVinculoRequest(Guid PacienteId, string DiasSemana);
record CriarQuestionarioRequest(Guid PsicologoId, string Titulo, List<PerguntaRequest> Perguntas, List<PacienteVinculoRequest> Pacientes);
record EditarQuestionarioRequest(string Titulo, List<PerguntaRequest> Perguntas, List<PacienteVinculoRequest> Pacientes);
record ResponderPerguntaRequest(Guid QuestionarioId, Guid PacienteId, int? ValorEscala, string? RespostaTexto, string? Observacao, string? AudioObservacao);
record SolicitarVinculoRequest(Guid PacienteId, Guid PsicologoId);

record CadastroPacienteRequest(string Nome, string Email, string Senha, string? Telefone, DateTime? DataNascimento, string? Genero);
record CadastroPsicologoRequest(string Nome, string Email, string Senha, string Crp, string? Telefone, DateTime? DataNascimento, string? Genero);
record LoginRequest(string Email, string Senha);
record AtualizarPacienteRequest(string Nome, string Email, string? Telefone, DateTime? DataNascimento, string? Genero, string? FotoUrl);
record AtualizarPsicologoRequest(string Nome, string Email, string? Telefone, DateTime? DataNascimento, string? Genero, string? FotoUrl, string Crp);