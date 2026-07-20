using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace MauiApp1.Services
{
	/// <summary>
	/// Envia um arquivo local (foto, áudio, documento) pro endpoint
	/// /api/upload da API, e recebe de volta o caminho onde ficou salvo
	/// no servidor — esse caminho é o que vai dentro da Mensagem.
	/// </summary>
	public class ArquivoUploadService
	{
		private readonly HttpClient _http = new();

		public async Task<(string Caminho, string NomeOriginal)> EnviarAsync(string caminhoLocal, string nomeArquivo)
		{
			using var conteudo = new MultipartFormDataContent();
			await using var stream = File.OpenRead(caminhoLocal);
			using var streamContent = new StreamContent(stream);

			conteudo.Add(streamContent, "arquivo", nomeArquivo);

			var resposta = await _http.PostAsync($"{ApiConfig.ServidorBaseUrl}/api/upload", conteudo);
			resposta.EnsureSuccessStatusCode();

			var json = await resposta.Content.ReadFromJsonAsync<JsonElement>();
			var caminho = json.GetProperty("caminho").GetString()!;
			var nomeOriginal = json.GetProperty("nomeOriginal").GetString()!;

			return (caminho, nomeOriginal);
		}

		/// <summary>Baixa um arquivo do servidor pro celular (cache local),
		/// usado pra tocar áudio — o player precisa de um arquivo local,
		/// não toca direto de uma URL remota do jeito que configuramos.
		/// Se já baixou antes, reaproveita (não baixa de novo).</summary>
		public async Task<string> BaixarAsync(string urlCompleta)
		{
			var nomeArquivo = Path.GetFileName(new Uri(urlCompleta).LocalPath);
			var caminhoLocal = Path.Combine(FileSystem.CacheDirectory, nomeArquivo);

			if (!File.Exists(caminhoLocal))
			{
				var bytes = await _http.GetByteArrayAsync(urlCompleta);
				await File.WriteAllBytesAsync(caminhoLocal, bytes);
			}

			return caminhoLocal;
		}
	}
}