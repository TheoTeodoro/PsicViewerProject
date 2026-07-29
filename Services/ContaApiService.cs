using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace MauiApp1.Services
{

	public class ContaApiService
	{
		private readonly HttpClient _http = new();
		private static readonly JsonSerializerOptions _opcoesJson = new() { PropertyNameCaseInsensitive = true };

		public Task<(bool Sucesso, UsuarioDto? Usuario, string? Erro)> CadastrarPacienteAsync(
			string nome, string email, string senha, string? telefone, DateTime? dataNascimento, string? genero)
			=> PostAsync<UsuarioDto>("/api/conta/cadastrar-paciente",
				new { nome, email, senha, telefone, dataNascimento, genero });

		public Task<(bool Sucesso, UsuarioDto? Usuario, string? Erro)> CadastrarPsicologoAsync(
			string nome, string email, string senha, string crp, string? telefone, DateTime? dataNascimento, string? genero)
			=> PostAsync<UsuarioDto>("/api/conta/cadastrar-psicologo",
				new { nome, email, senha, crp, telefone, dataNascimento, genero });

		public Task<(bool Sucesso, UsuarioDto? Usuario, string? Erro)> LoginAsync(string email, string senha)
			=> PostAsync<UsuarioDto>("/api/conta/login", new { email, senha });

		public async Task<UsuarioDto?> ObterPacienteAsync(Guid id)
		{
			var resposta = await _http.GetAsync($"{ApiConfig.ServidorBaseUrl}/api/conta/paciente/{id}");
			if (!resposta.IsSuccessStatusCode) return null;
			return await resposta.Content.ReadFromJsonAsync<UsuarioDto>(_opcoesJson);
		}

		public async Task<UsuarioDto?> ObterPsicologoAsync(Guid id)
		{
			var resposta = await _http.GetAsync($"{ApiConfig.ServidorBaseUrl}/api/conta/psicologo/{id}");
			if (!resposta.IsSuccessStatusCode) return null;
			return await resposta.Content.ReadFromJsonAsync<UsuarioDto>(_opcoesJson);
		}

		public Task<(bool Sucesso, string? Erro)> AtualizarPacienteAsync(
			Guid id, string nome, string email, string? telefone, DateTime? dataNascimento, string? genero, string? fotoUrl)
			=> PutAsync($"/api/conta/paciente/{id}", new { nome, email, telefone, dataNascimento, genero, fotoUrl });

		public Task<(bool Sucesso, string? Erro)> AtualizarPsicologoAsync(
			Guid id, string nome, string email, string? telefone, DateTime? dataNascimento, string? genero, string? fotoUrl, string crp)
			=> PutAsync($"/api/conta/psicologo/{id}", new { nome, email, telefone, dataNascimento, genero, fotoUrl, crp });

		public async Task<List<UsuarioDto>> ListarPacientesAsync()
		{
			var resultado = await _http.GetFromJsonAsync<List<UsuarioDto>>(
				$"{ApiConfig.ServidorBaseUrl}/api/conta/pacientes", _opcoesJson);
			return resultado ?? new List<UsuarioDto>();
		}

		public async Task<List<UsuarioDto>> ListarPsicologosAsync()
		{
			var resultado = await _http.GetFromJsonAsync<List<UsuarioDto>>(
				$"{ApiConfig.ServidorBaseUrl}/api/conta/psicologos", _opcoesJson);
			return resultado ?? new List<UsuarioDto>();
		}

		private async Task<(bool, T?, string?)> PostAsync<T>(string caminho, object corpo)
		{
			var resposta = await _http.PostAsJsonAsync($"{ApiConfig.ServidorBaseUrl}{caminho}", corpo);

			if (resposta.IsSuccessStatusCode)
			{
				var dados = await resposta.Content.ReadFromJsonAsync<T>(_opcoesJson);
				return (true, dados, null);
			}

			var erro = await LerErroAsync(resposta);
			return (false, default, erro);
		}

		private async Task<(bool, string?)> PutAsync(string caminho, object corpo)
		{
			var resposta = await _http.PutAsJsonAsync($"{ApiConfig.ServidorBaseUrl}{caminho}", corpo);

			if (resposta.IsSuccessStatusCode)
				return (true, null);

			var erro = await LerErroAsync(resposta);
			return (false, erro);
		}

		private static async Task<string?> LerErroAsync(HttpResponseMessage resposta)
		{
			try
			{
				var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
				if (corpo.TryGetProperty("erro", out var erroProp))
					return erroProp.GetString();
			}
			catch { }

			return $"Erro do servidor ({(int)resposta.StatusCode}).";
		}
	}
}