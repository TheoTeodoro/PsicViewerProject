using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace MauiApp1.Services
{
	public record PerguntaParaCriar(Guid? Id, string Tipo, string Texto, string? Opcoes, string Horario, bool Ativa);
	public record PacienteVinculoParaCriar(Guid PacienteId, string DiasSemana);

	public class QuestionarioApiService
	{
		private readonly HttpClient _http = new();
		private static readonly JsonSerializerOptions _opcoesJson = new() { PropertyNameCaseInsensitive = true };

		public async Task<(bool Sucesso, QuestionarioDto? Questionario, string? Erro)> CriarAsync(
			Guid psicologoId, string titulo, List<PerguntaParaCriar> perguntas, List<PacienteVinculoParaCriar> pacientes)
		{
			var resposta = await _http.PostAsJsonAsync($"{ApiConfig.ServidorBaseUrl}/api/questionario/criar",
				new { psicologoId, titulo, perguntas, pacientes });

			if (resposta.IsSuccessStatusCode)
			{
				var dados = await resposta.Content.ReadFromJsonAsync<QuestionarioDto>(_opcoesJson);
				return (true, dados, null);
			}

			string? erro;
			try
			{
				var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
				erro = corpo.TryGetProperty("erro", out var e) ? e.GetString() : null;
			}
			catch { erro = null; }

			return (false, null, erro ?? $"Erro do servidor ({(int)resposta.StatusCode}).");
		}

		public async Task<List<QuestionarioDto>> ListarPorPsicologoAsync(Guid psicologoId)
		{
			var resultado = await _http.GetFromJsonAsync<List<QuestionarioDto>>(
				$"{ApiConfig.ServidorBaseUrl}/api/questionario/psicologo/{psicologoId}", _opcoesJson);
			return resultado ?? new List<QuestionarioDto>();
		}

		public async Task<QuestionarioEdicaoDto?> ObterParaEditarAsync(Guid questionarioId)
		{
			try
			{
				return await _http.GetFromJsonAsync<QuestionarioEdicaoDto>(
					$"{ApiConfig.ServidorBaseUrl}/api/questionario/{questionarioId}/editar", _opcoesJson);
			}
			catch
			{
				return null;
			}
		}

		public async Task<(bool Sucesso, string? Erro)> EditarAsync(Guid questionarioId, string titulo,
			List<PerguntaParaCriar> perguntas, List<PacienteVinculoParaCriar> pacientes)
		{
			var resposta = await _http.PutAsJsonAsync($"{ApiConfig.ServidorBaseUrl}/api/questionario/{questionarioId}",
				new { titulo, perguntas, pacientes });

			if (resposta.IsSuccessStatusCode) return (true, null);

			string? erro;
			try
			{
				var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
				erro = corpo.TryGetProperty("erro", out var e) ? e.GetString() : null;
			}
			catch { erro = null; }

			return (false, erro ?? $"Erro do servidor ({(int)resposta.StatusCode}).");
		}

		/// <summary>Apaga o questionário de vez (perguntas, respostas e
		/// vínculos com pacientes vão junto) — diferente de arquivar,
		/// não tem volta.</summary>
		public async Task<(bool Sucesso, string? Erro)> ExcluirAsync(Guid questionarioId)
		{
			var resposta = await _http.DeleteAsync($"{ApiConfig.ServidorBaseUrl}/api/questionario/{questionarioId}");

			if (resposta.IsSuccessStatusCode) return (true, null);

			string? erro;
			try
			{
				var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
				erro = corpo.TryGetProperty("erro", out var e) ? e.GetString() : null;
			}
			catch { erro = null; }

			return (false, erro ?? $"Erro do servidor ({(int)resposta.StatusCode}).");
		}

		/// <summary>Quantos TIPOS distintos de questionário (ativos) estão
		/// realmente em uso por algum paciente — pro Sumário Clínico da Home.</summary>
		public async Task<int> ObterQuestionariosEmUsoAsync(Guid psicologoId)
		{
			try
			{
				var resposta = await _http.GetFromJsonAsync<JsonElement>(
					$"{ApiConfig.ServidorBaseUrl}/api/questionario/psicologo/{psicologoId}/resumo");
				return resposta.TryGetProperty("questionariosAtivosEmUso", out var v) ? v.GetInt32() : 0;
			}
			catch
			{
				return 0;
			}
		}

		/// <summary>Quantas perguntas ainda não foram respondidas HOJE,
		/// somando todos os pacientes — pro Sumário Clínico da Home.</summary>
		public async Task<int> ObterPerguntasPendentesHojeAsync(Guid psicologoId)
		{
			try
			{
				var resposta = await _http.GetFromJsonAsync<JsonElement>(
					$"{ApiConfig.ServidorBaseUrl}/api/questionario/psicologo/{psicologoId}/pendentes-hoje");
				return resposta.TryGetProperty("pendentes", out var v) ? v.GetInt32() : 0;
			}
			catch
			{
				return 0;
			}
		}

		// ── Lado do Paciente ────────────────────────────────────────

		public async Task<List<QuestionarioParaPacienteDto>> ListarPorPacienteAsync(Guid pacienteId)
		{
			var resposta = await _http.GetAsync($"{ApiConfig.ServidorBaseUrl}/api/questionario/paciente/{pacienteId}");

			if (!resposta.IsSuccessStatusCode)
				throw new HttpRequestException($"Erro do servidor ({(int)resposta.StatusCode}) ao listar questionários do paciente.");

			var resultado = await resposta.Content.ReadFromJsonAsync<List<QuestionarioParaPacienteDto>>(_opcoesJson);
			return resultado ?? new List<QuestionarioParaPacienteDto>();
		}

		/// <summary>Card "Próxima pergunta" da Home — questionário ativo
		/// mais antigo pendente hoje, e nele a pergunta ainda não
		/// respondida cujo horário está mais perto de agora.</summary>
		public async Task<ProximaPerguntaDto?> ObterProximaPerguntaAsync(Guid pacienteId)
		{
			try
			{
				return await _http.GetFromJsonAsync<ProximaPerguntaDto>(
					$"{ApiConfig.ServidorBaseUrl}/api/questionario/paciente/{pacienteId}/proxima-pergunta", _opcoesJson);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>Respostas de dias anteriores a hoje — o "Histórico".</summary>
		public async Task<List<ItemHistoricoDto>> ListarHistoricoAsync(Guid pacienteId)
		{
			var resposta = await _http.GetAsync($"{ApiConfig.ServidorBaseUrl}/api/questionario/paciente/{pacienteId}/historico");

			if (!resposta.IsSuccessStatusCode)
				throw new HttpRequestException($"Erro do servidor ({(int)resposta.StatusCode}) ao listar o histórico.");

			var resultado = await resposta.Content.ReadFromJsonAsync<List<ItemHistoricoDto>>(_opcoesJson);
			return resultado ?? new List<ItemHistoricoDto>();
		}

		/// <summary>Detalhe completo de um questionário respondido num dia
		/// específico do histórico — todas as perguntas, com "Sem
		/// resposta" pras que não foram respondidas naquele dia. Só
		/// leitura, não dá pra editar uma resposta de um dia que já
		/// passou. Devolve o erro de verdade (status + corpo da resposta,
		/// ou a exceção) em vez de engolir tudo — antes, qualquer falha
		/// virava só "Não foi possível carregar esse questionário",
		/// sem pista nenhuma do motivo real.</summary>
		public async Task<(QuestionarioHistoricoDetalheDto? Detalhe, string? Erro)> ObterHistoricoDetalheAsync(Guid questionarioId, Guid pacienteId, string data)
		{
			try
			{
				var resposta = await _http.GetAsync($"{ApiConfig.ServidorBaseUrl}/api/questionario/{questionarioId}/paciente/{pacienteId}/dia/{data}");

				if (!resposta.IsSuccessStatusCode)
				{
					var corpo = await resposta.Content.ReadAsStringAsync();
					return (null, $"Erro do servidor ({(int)resposta.StatusCode}): {corpo}");
				}

				var detalhe = await resposta.Content.ReadFromJsonAsync<QuestionarioHistoricoDetalheDto>(_opcoesJson);
				return (detalhe, detalhe is null ? "O servidor respondeu sem dados." : null);
			}
			catch (Exception ex)
			{
				return (null, $"{ex.GetType().Name}: {ex.Message}");
			}
		}

		/// <summary>Histórico de um paciente, visto pelo PSICÓLOGO — só os
		/// questionários que ele mesmo vinculou a esse paciente, nunca de
		/// outro psicólogo.</summary>
		public async Task<List<ItemHistoricoDto>> ListarHistoricoPacientePorPsicologoAsync(Guid psicologoId, Guid pacienteId)
		{
			var resposta = await _http.GetAsync(
				$"{ApiConfig.ServidorBaseUrl}/api/questionario/psicologo/{psicologoId}/paciente/{pacienteId}/historico");

			if (!resposta.IsSuccessStatusCode)
				throw new HttpRequestException($"Erro do servidor ({(int)resposta.StatusCode}) ao listar o histórico do paciente.");

			var resultado = await resposta.Content.ReadFromJsonAsync<List<ItemHistoricoDto>>(_opcoesJson);
			return resultado ?? new List<ItemHistoricoDto>();
		}

		public async Task<List<PontoHumorDto>> ObterRelatorioHumorAsync(Guid psicologoId, Guid pacienteId, DateTime? inicio = null, DateTime? fim = null)
		{
			var url = $"{ApiConfig.ServidorBaseUrl}/api/questionario/psicologo/{psicologoId}/paciente/{pacienteId}/relatorio-humor";

			var query = new List<string>();
			if (inicio.HasValue) query.Add($"inicio={inicio.Value:yyyy-MM-dd}");
			if (fim.HasValue) query.Add($"fim={fim.Value:yyyy-MM-dd}");
			if (query.Count > 0) url += "?" + string.Join("&", query);

			var resposta = await _http.GetAsync(url);

			if (!resposta.IsSuccessStatusCode)
				throw new HttpRequestException($"Erro do servidor ({(int)resposta.StatusCode}) ao carregar o relatório.");

			var resultado = await resposta.Content.ReadFromJsonAsync<List<PontoHumorDto>>(_opcoesJson);
			return resultado ?? new List<PontoHumorDto>();
		}

	
		public async Task<List<PontoSerieDto>> ObterSeriePerguntaAsync(Guid psicologoId, Guid pacienteId, Guid perguntaId, DateTime? inicio = null, DateTime? fim = null)
		{
			var url = $"{ApiConfig.ServidorBaseUrl}/api/questionario/psicologo/{psicologoId}/paciente/{pacienteId}/pergunta/{perguntaId}/serie";

			var query = new List<string>();
			if (inicio.HasValue) query.Add($"inicio={inicio.Value:yyyy-MM-dd}");
			if (fim.HasValue) query.Add($"fim={fim.Value:yyyy-MM-dd}");
			if (query.Count > 0) url += "?" + string.Join("&", query);

			var resposta = await _http.GetAsync(url);

			if (!resposta.IsSuccessStatusCode)
				throw new HttpRequestException($"Erro do servidor ({(int)resposta.StatusCode}) ao carregar a série da pergunta.");

			var resultado = await resposta.Content.ReadFromJsonAsync<List<PontoSerieDto>>(_opcoesJson);
			return resultado ?? new List<PontoSerieDto>();
		}

		public async Task<QuestionarioParaResponderDto?> ObterParaResponderAsync(Guid questionarioId, Guid pacienteId)
		{
			try
			{
				return await _http.GetFromJsonAsync<QuestionarioParaResponderDto>(
					$"{ApiConfig.ServidorBaseUrl}/api/questionario/{questionarioId}/responder/{pacienteId}", _opcoesJson);
			}
			catch
			{
				return null;
			}
		}

		public async Task<(bool Sucesso, string? Erro)> ResponderPerguntaAsync(Guid questionarioId, Guid perguntaId, Guid pacienteId,
			int? valorEscala, string? respostaTexto, string? observacao, string? audioObservacao)
		{
			var resposta = await _http.PostAsJsonAsync(
				$"{ApiConfig.ServidorBaseUrl}/api/questionario/pergunta/{perguntaId}/responder",
				new { questionarioId, pacienteId, valorEscala, respostaTexto, observacao, audioObservacao });

			if (resposta.IsSuccessStatusCode) return (true, null);

			string? erro;
			try
			{
				var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
				erro = corpo.TryGetProperty("erro", out var e) ? e.GetString() : null;
			}
			catch { erro = null; }

			return (false, erro ?? $"Erro do servidor ({(int)resposta.StatusCode}).");
		}

		public async Task<List<JsonElement>> ObterRespostasNaoVistasAsync(Guid psicologoId)
		{
			try
			{
				var resultado = await _http.GetFromJsonAsync<List<JsonElement>>(
					$"{ApiConfig.ServidorBaseUrl}/api/questionario/psicologo/{psicologoId}/respostas-nao-vistas");
				return resultado ?? new List<JsonElement>();
			}
			catch
			{
				return new List<JsonElement>();
			}
		}

		public async Task<RespostaDetalheDto?> ObterDetalheRespostaAsync(Guid respostaId)
		{
			try
			{
				return await _http.GetFromJsonAsync<RespostaDetalheDto>(
					$"{ApiConfig.ServidorBaseUrl}/api/resposta/{respostaId}/detalhe", _opcoesJson);
			}
			catch
			{
				return null;
			}
		}

		public async Task MarcarRespostaVisualizadaAsync(Guid respostaId)
		{
			await _http.PostAsync($"{ApiConfig.ServidorBaseUrl}/api/resposta/{respostaId}/marcar-visualizada", null);
		}
	}
}