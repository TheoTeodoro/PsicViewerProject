using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace MauiApp1.Services
{
	public enum TipoNotificacao
	{
		SolicitacaoVinculo,
		VinculoAceito,
		Mensagem,
		Audio,
		Imagem,
		Documento,
		RespostaQuestionario,
		Feedback
	}

	public class ItemNotificacao
	{
		public string Chave { get; set; } = string.Empty;
		public TipoNotificacao Tipo { get; set; }
		public string NomeRemetente { get; set; } = string.Empty;
		public string Descricao { get; set; } = string.Empty;
		public DateTime Quando { get; set; }
		public bool NaoLida { get; set; }
		public Guid VinculoId { get; set; }
		public Guid ContatoId { get; set; }
		public string ContatoNome { get; set; } = string.Empty;

		public string Icone => Tipo switch
		{
			TipoNotificacao.SolicitacaoVinculo => "🤝",
			TipoNotificacao.VinculoAceito => "✅",
			TipoNotificacao.Mensagem => "💬",
			TipoNotificacao.Audio => "🎤",
			TipoNotificacao.Imagem => "🖼️",
			TipoNotificacao.Documento => "📄",
			TipoNotificacao.RespostaQuestionario => "📋",
			TipoNotificacao.Feedback => "📝",
			_ => "🔔"
		};

		public string HorarioExibido
		{
			get
			{
				var diferencaDias = (DateTime.Now.Date - Quando.Date).Days;
				if (diferencaDias == 0) return Quando.ToString("HH:mm");
				if (diferencaDias == 1) return "Ontem";
				return Quando.ToString("dd/MM");
			}
		}
	}

	public class NotificacaoService
	{
		private readonly VinculoApiService _vinculo;
		private readonly QuestionarioApiService _questionarios;
		private readonly ChatConnectionService _chat;
		private readonly SessaoUsuario _sessao;
		private readonly HttpClient _http = new();

		
		private DateTime _ultimaVisualizacao = DateTime.MinValue;

		public NotificacaoService(VinculoApiService vinculo, QuestionarioApiService questionarios, ChatConnectionService chat, SessaoUsuario sessao)
		{
			_vinculo = vinculo;
			_questionarios = questionarios;
			_chat = chat;
			_sessao = sessao;
		}

		
		public void MarcarListaComoVisualizadaAgora() => _ultimaVisualizacao = DateTime.Now;

		public async Task<List<ItemNotificacao>> ObterNotificacoesAsync()
		{
			var itens = new List<ItemNotificacao>();

			if (_sessao.Tipo == TipoUsuarioLogado.Paciente)
			{
				var vinculos = await _vinculo.ListarPorPacienteAsync(_sessao.UsuarioId);
				AdicionarEventosDeVinculo(itens, vinculos, origemPedido: "Psicologo", origemAceito: "Paciente");
			}
			else if (_sessao.Tipo == TipoUsuarioLogado.Psicologo)
			{
				var vinculos = await _vinculo.ListarPorPsicologoAsync(_sessao.UsuarioId);
				AdicionarEventosDeVinculo(itens, vinculos, origemPedido: "Paciente", origemAceito: "Psicologo");

				itens.AddRange(await ObterNotificacoesDeRespostasAsync());
			}

			itens.AddRange(await ObterNotificacoesDeMensagensAsync());

			
			foreach (var item in itens)
				item.NaoLida = item.Quando > _ultimaVisualizacao;

			return itens.OrderByDescending(i => i.Quando).ToList();
		}

		public async Task LimparTodasAsync(IEnumerable<ItemNotificacao> itens)
		{
			var remetentesParaMarcar = new HashSet<Guid>();

			foreach (var item in itens)
			{
				if (item.Tipo == TipoNotificacao.SolicitacaoVinculo)
					await _vinculo.MarcarPedidoVisualizadoAsync(item.VinculoId);
				else if (item.Tipo == TipoNotificacao.VinculoAceito)
					await _vinculo.MarcarAceitoVisualizadoAsync(item.VinculoId);
				else if (item.Tipo is TipoNotificacao.Mensagem or TipoNotificacao.Audio or TipoNotificacao.Imagem or TipoNotificacao.Documento or TipoNotificacao.Feedback)
					remetentesParaMarcar.Add(item.ContatoId);
			}

			foreach (var remetenteId in remetentesParaMarcar)
				await _chat.MarcarComoLidasAsync(remetenteId, _sessao.UsuarioId);

			MarcarListaComoVisualizadaAgora();
		}

	
		private async Task<List<ItemNotificacao>> ObterNotificacoesDeRespostasAsync()
		{
			var lista = new List<ItemNotificacao>();
			try
			{
				var respostas = await _questionarios.ObterRespostasNaoVistasAsync(_sessao.UsuarioId);

				foreach (var item in respostas)
				{
					var respostaId = Guid.Parse(item.GetProperty("respostaId").GetString()!);
					var pacienteId = Guid.Parse(item.GetProperty("pacienteId").GetString()!);
					var pacienteNome = item.GetProperty("pacienteNome").GetString() ?? "Alguém";
					var questionarioTitulo = item.GetProperty("questionarioTitulo").GetString() ?? string.Empty;
					var perguntaTexto = item.TryGetProperty("perguntaTexto", out var pt) ? pt.GetString() ?? string.Empty : string.Empty;
					var respondidoEm = ParseDataServidor(item.GetProperty("respondidoEm").GetString());

					lista.Add(new ItemNotificacao
					{
						Chave = $"resposta-{respostaId}",
						Tipo = TipoNotificacao.RespostaQuestionario,
						NomeRemetente = pacienteNome,
						Descricao = string.IsNullOrEmpty(perguntaTexto)
							? $"Respondeu \"{questionarioTitulo}\""
							: $"Respondeu: \"{perguntaTexto}\"",
						Quando = respondidoEm,
						NaoLida = true,
						VinculoId = respostaId, 
						ContatoId = pacienteId,
						ContatoNome = pacienteNome
					});
				}
			}
			catch
			{
			}

			return lista;
		}

		private static void AdicionarEventosDeVinculo(List<ItemNotificacao> itens, List<VinculoDto> vinculos, string origemPedido, string origemAceito)
		{
			foreach (var v in vinculos.Where(v => v.Status == "Pendente" && v.Origem == origemPedido))
			{
				itens.Add(new ItemNotificacao
				{
					Chave = $"{v.Id}-pedido",
					Tipo = TipoNotificacao.SolicitacaoVinculo,
					NomeRemetente = v.ContatoNome,
					Descricao = "Solicitou vínculo",
					Quando = v.SolicitadoEm.ToLocalTime(),
					NaoLida = !v.PedidoVisualizado,
					VinculoId = v.Id,
					ContatoId = v.ContatoId,
					ContatoNome = v.ContatoNome
				});
			}

			foreach (var v in vinculos.Where(v => v.Status == "Aceito" && v.Origem == origemAceito))
			{
				itens.Add(new ItemNotificacao
				{
					Chave = $"{v.Id}-aceito",
					Tipo = TipoNotificacao.VinculoAceito,
					NomeRemetente = v.ContatoNome,
					Descricao = "Aceitou sua solicitação",
					Quando = (v.RespondidoEm ?? v.SolicitadoEm).ToLocalTime(),
					NaoLida = !v.AceitoVisualizado,
					VinculoId = v.Id,
					ContatoId = v.ContatoId,
					ContatoNome = v.ContatoNome
				});
			}
		}

		private async Task<List<ItemNotificacao>> ObterNotificacoesDeMensagensAsync()
		{
			var lista = new List<ItemNotificacao>();
			List<JsonElement>? resultado;

			try
			{
				resultado = await _http.GetFromJsonAsync<List<JsonElement>>(
					$"{ApiConfig.ServidorBaseUrl}/api/mensagens/nao-lidas/{_sessao.UsuarioId}");
			}
			catch
			{
				return lista;
			}

			if (resultado is null) return lista;

			
			foreach (var item in resultado)
			{
				try
				{
					var itemNotificacao = ParseMensagemNaoLida(item);
					if (itemNotificacao is not null)
						lista.Add(itemNotificacao);
				}
				catch
				{
				}
			}

			return lista;
		}

		private static ItemNotificacao? ParseMensagemNaoLida(JsonElement item)
		{
			if (!item.TryGetProperty("mensagemId", out var mensagemIdProp)) return null;
			var mensagemId = Guid.Parse(mensagemIdProp.GetString()!);

			if (!item.TryGetProperty("remetenteId", out var remetenteIdProp)) return null;
			var remetenteId = Guid.Parse(remetenteIdProp.GetString()!);

			var remetenteNome = item.TryGetProperty("remetenteNome", out var rn) ? rn.GetString() ?? "Alguém" : "Alguém";
			var tipoStr = item.TryGetProperty("tipoConteudo", out var tc) ? tc.GetString() ?? "Texto" : "Texto";

			string? nomeArquivo = null;
			if (item.TryGetProperty("nomeArquivoOriginal", out var na) && na.ValueKind == JsonValueKind.String)
				nomeArquivo = na.GetString();

			var enviadaEm = ParseDataServidor(item.GetProperty("enviadaEm").GetString());

			var ehFeedback = item.TryGetProperty("ehFeedback", out var fb) && fb.ValueKind == JsonValueKind.True;
			if (ehFeedback)
			{
				return new ItemNotificacao
				{
					Chave = $"msg-{mensagemId}",
					Tipo = TipoNotificacao.Feedback,
					NomeRemetente = remetenteNome,
					Descricao = "Deu um feedback à sua resposta",
					Quando = enviadaEm,
					NaoLida = true,
					ContatoId = remetenteId,
					ContatoNome = remetenteNome
				};
			}

			var (tipo, descricao) = tipoStr switch
			{
				"Audio" => (TipoNotificacao.Audio, "Mensagem de áudio"),
				"Imagem" => (TipoNotificacao.Imagem, "Foto enviada"),
				"Documento" => (TipoNotificacao.Documento, TruncarNomeArquivo(nomeArquivo)),
				_ => (TipoNotificacao.Mensagem, "Enviou uma mensagem")
			};

			return new ItemNotificacao
			{
				Chave = $"msg-{mensagemId}",
				Tipo = tipo,
				NomeRemetente = remetenteNome,
				Descricao = descricao,
				Quando = enviadaEm,
				NaoLida = true,
				ContatoId = remetenteId,
				ContatoNome = remetenteNome
			};
		}

		private static DateTime ParseDataServidor(string? valor)
		{
			if (string.IsNullOrWhiteSpace(valor))
				return DateTime.Now;

			var data = DateTime.Parse(valor, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

			if (data.Kind == DateTimeKind.Unspecified)
				data = DateTime.SpecifyKind(data, DateTimeKind.Utc);

			return data.ToLocalTime();
		}

		private static string TruncarNomeArquivo(string? nome)
		{
			if (string.IsNullOrEmpty(nome)) return "Documento";

			const int maxLen = 22;
			if (nome.Length <= maxLen) return nome;

			var extensao = Path.GetExtension(nome);
			var semExtensao = Path.GetFileNameWithoutExtension(nome);
			var disponivel = Math.Max(1, maxLen - extensao.Length - 3);

			return semExtensao.Substring(0, Math.Min(disponivel, semExtensao.Length)) + "..." + extensao;
		}

	
		public async Task MarcarTodasComoVistasAsync(IEnumerable<ItemNotificacao> itens)
		{
			foreach (var item in itens)
			{
				if (item.Tipo == TipoNotificacao.SolicitacaoVinculo)
					await _vinculo.MarcarPedidoVisualizadoAsync(item.VinculoId);
				else if (item.Tipo == TipoNotificacao.VinculoAceito)
					await _vinculo.MarcarAceitoVisualizadoAsync(item.VinculoId);
			}
		}
	}
}