using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace MauiApp1.Services
{
	/// <summary>Fala com os endpoints de vínculo Paciente-Psicólogo (RF03) —
	/// solicitar, aceitar, recusar e listar.</summary>
	public class VinculoApiService
	{
		private readonly HttpClient _http = new();
		private static readonly JsonSerializerOptions _opcoesJson = new() { PropertyNameCaseInsensitive = true };

		public async Task<(bool Sucesso, string? Erro)> SolicitarAsync(Guid pacienteId, Guid psicologoId)
		{
			var resposta = await _http.PostAsJsonAsync(
				$"{ApiConfig.ServidorBaseUrl}/api/vinculo/solicitar", new { pacienteId, psicologoId });

			if (resposta.IsSuccessStatusCode) return (true, null);
			return (false, await LerErroAsync(resposta));
		}

		/// <summary>Mesma solicitação, mas iniciada pelo Psicólogo (busca
		/// paciente e convida) — quem precisa responder depois é o
		/// Paciente, não o Psicólogo.</summary>
		public async Task<(bool Sucesso, string? Erro)> SolicitarComoPsicologoAsync(Guid psicologoId, Guid pacienteId)
		{
			var resposta = await _http.PostAsJsonAsync(
				$"{ApiConfig.ServidorBaseUrl}/api/vinculo/psicologo-solicitar", new { pacienteId, psicologoId });

			if (resposta.IsSuccessStatusCode) return (true, null);
			return (false, await LerErroAsync(resposta));
		}

		public async Task<bool> AceitarAsync(Guid vinculoId)
		{
			var resposta = await _http.PostAsync($"{ApiConfig.ServidorBaseUrl}/api/vinculo/{vinculoId}/aceitar", null);
			return resposta.IsSuccessStatusCode;
		}

		public async Task<bool> RecusarAsync(Guid vinculoId)
		{
			var resposta = await _http.PostAsync($"{ApiConfig.ServidorBaseUrl}/api/vinculo/{vinculoId}/recusar", null);
			return resposta.IsSuccessStatusCode;
		}

		public async Task<List<VinculoDto>> ListarPorPacienteAsync(Guid pacienteId)
		{
			var resultado = await _http.GetFromJsonAsync<List<VinculoDto>>(
				$"{ApiConfig.ServidorBaseUrl}/api/vinculo/paciente/{pacienteId}", _opcoesJson);
			return resultado ?? new List<VinculoDto>();
		}

		public async Task<List<VinculoDto>> ListarPorPsicologoAsync(Guid psicologoId)
		{
			var resultado = await _http.GetFromJsonAsync<List<VinculoDto>>(
				$"{ApiConfig.ServidorBaseUrl}/api/vinculo/psicologo/{psicologoId}", _opcoesJson);
			return resultado ?? new List<VinculoDto>();
		}

		private static async Task<string?> LerErroAsync(HttpResponseMessage resposta)
		{
			try
			{
				var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
				if (corpo.TryGetProperty("erro", out var erroProp))
					return erroProp.GetString();
			}
			catch { /* corpo não era JSON com "erro" */ }

			return $"Erro do servidor ({(int)resposta.StatusCode}).";
		}
	}
}