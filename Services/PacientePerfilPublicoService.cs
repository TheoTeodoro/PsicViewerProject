using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace MauiApp1.Services
{
	/// <summary>Só os dados que o psicólogo pode ver de um paciente ao
	/// tocar nele na lista — sem e-mail, telefone ou data de nascimento
	/// exata (o servidor já filtra isso em /perfil-publico).</summary>
	public class PerfilPublicoPacienteDto
	{
		public Guid Id { get; set; }
		public string Nome { get; set; } = string.Empty;
		public string? FotoUrl { get; set; }
		public int? Idade { get; set; }
		public string? Genero { get; set; }
	}

	public class PacientePerfilPublicoService
	{
		private readonly HttpClient _http = new();

		public async Task<PerfilPublicoPacienteDto?> ObterAsync(Guid pacienteId)
		{
			try
			{
				return await _http.GetFromJsonAsync<PerfilPublicoPacienteDto>(
					$"{ApiConfig.ServidorBaseUrl}/api/conta/paciente/{pacienteId}/perfil-publico");
			}
			catch
			{
				return null;
			}
		}
	}
}