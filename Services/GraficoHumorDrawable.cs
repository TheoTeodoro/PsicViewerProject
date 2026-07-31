using System.Globalization;
using Microsoft.Maui.Graphics;

namespace MauiApp1.Services
{
	public class PontoGrafico
	{
		public DateTime Data { get; set; }
		public double Valor { get; set; }
		public string? Observacao { get; set; }
		public string? AudioObservacao { get; set; }
	}

	public class SerieEscala
	{
		public string Nome { get; set; } = string.Empty;
		public Color Cor { get; set; } = Colors.Black;
		public List<PontoGrafico> Pontos { get; set; } = new();
	}

	public class SerieDestaque
	{
		public string Nome { get; set; } = string.Empty;
		public Dictionary<DateTime, string> RespostasPorDia { get; set; } = new();
	}

	public readonly record struct PontoDesenhado(PointF Centro, SerieEscala Serie, PontoGrafico Ponto);

	public class GraficoHumorDrawable : IDrawable
	{
		public List<SerieEscala> Series { get; set; } = new();
		public List<SerieDestaque> Destaques { get; set; } = new();

		public List<PontoDesenhado> PontosDesenhados { get; } = new();

		private static readonly Color CorGrade = Color.FromArgb("#E0E0E0");
		private static readonly Color CorEixo = Color.FromArgb("#9B9C96");
		private static readonly Color CorRotulo = Color.FromArgb("#1C3D5A");

		public static readonly Color CorSim = Color.FromArgb("#1F9D55");
		public static readonly Color CorNao = Color.FromArgb("#D9534F");
		public static readonly Color CorMaisOuMenos = Color.FromArgb("#E8A33D");
		public static readonly Color CorNaoRespondeu = Color.FromArgb("#B0B0B0");

		private static readonly Color[] PaletaDestaque =
		{
			Color.FromArgb("#7B61FF"),
			Color.FromArgb("#3D8BE8"),
			Color.FromArgb("#E8D53D"),
			Color.FromArgb("#E85DA0"),
			Color.FromArgb("#3DE8C9"),
			Color.FromArgb("#8D6E63"),
		};

		private static bool EhSim(string resposta) => resposta.Trim().Equals("sim", StringComparison.OrdinalIgnoreCase);
		private static bool EhNao(string resposta) => resposta.Trim().Equals("não", StringComparison.OrdinalIgnoreCase)
			|| resposta.Trim().Equals("nao", StringComparison.OrdinalIgnoreCase);
		private static bool EhMaisOuMenos(string resposta) => resposta.Trim().Equals("mais ou menos", StringComparison.OrdinalIgnoreCase);

		public static List<string> ObterOutrasOpcoes(IEnumerable<SerieDestaque> destaques)
		{
			return destaques
				.SelectMany(d => d.RespostasPorDia.Values)
				.Select(v => v.Trim())
				.Where(v => !EhSim(v) && !EhNao(v) && !EhMaisOuMenos(v))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(v => v, StringComparer.Create(new CultureInfo("pt-BR"), ignoreCase: true))
				.ToList();
		}

		public static Color CorParaResposta(string resposta, List<string> outrasOpcoes)
		{
			if (EhSim(resposta)) return CorSim;
			if (EhNao(resposta)) return CorNao;
			if (EhMaisOuMenos(resposta)) return CorMaisOuMenos;

			var indice = outrasOpcoes.FindIndex(o => o.Equals(resposta.Trim(), StringComparison.OrdinalIgnoreCase));
			if (indice < 0) return PaletaDestaque[0];

			return PaletaDestaque[indice % PaletaDestaque.Length];
		}

		public static List<(string Texto, Color Cor)> ObterLegenda(List<SerieDestaque> destaques, int totalDiasNoPeriodo)
		{
			var itens = new List<(string Texto, Color Cor)>();
			var todasRespostas = destaques.SelectMany(d => d.RespostasPorDia.Values).Select(v => v.Trim()).ToList();
			var outrasOpcoes = ObterOutrasOpcoes(destaques);

			if (todasRespostas.Any(EhSim))
				itens.Add(("Sim", CorSim));
			if (todasRespostas.Any(EhNao))
				itens.Add(("Não", CorNao));
			if (todasRespostas.Any(EhMaisOuMenos))
				itens.Add(("Mais ou menos", CorMaisOuMenos));
			foreach (var opcao in outrasOpcoes)
				itens.Add((opcao, CorParaResposta(opcao, outrasOpcoes)));

			if (destaques.Any(d => d.RespostasPorDia.Count < totalDiasNoPeriodo))
				itens.Add(("Não respondeu", CorNaoRespondeu));

			return itens;
		}

		public void Draw(ICanvas canvas, RectF dirtyRect)
		{
			PontosDesenhados.Clear();

			var todasAsDatas = Series
				.SelectMany(s => s.Pontos.Select(p => p.Data.Date))
				.Distinct()
				.OrderBy(d => d)
				.ToList();

			if (todasAsDatas.Count == 0) return;

			const float padEsq = 34;
			const float padDireita = 16;
			const float padTopo = 14;
			const float alturaPorFaixaDestaque = 22;

			float alturaDestaques = Destaques.Count * alturaPorFaixaDestaque;
			float padBaixo = 26 + alturaDestaques;

			float largura = dirtyRect.Width - padEsq - padDireita;
			float altura = dirtyRect.Height - padTopo - padBaixo;

			canvas.StrokeColor = CorGrade;
			canvas.StrokeSize = 1;
			canvas.FontSize = 10;
			canvas.FontColor = CorEixo;

			for (int nivel = 1; nivel <= 5; nivel++)
			{
				float y = padTopo + altura - ((nivel - 1) / 4f) * altura;
				canvas.DrawLine(padEsq, y, dirtyRect.Width - padDireita, y);
				canvas.DrawString(nivel.ToString(), 4, y - 6, padEsq - 8, 12, HorizontalAlignment.Left, VerticalAlignment.Center);
			}

			float ValorParaY(double valor) => padTopo + altura - ((float)(valor - 1) / 4f) * altura;
			float IndiceParaX(int indice) => todasAsDatas.Count == 1
				? padEsq + largura / 2
				: padEsq + (largura * indice / (todasAsDatas.Count - 1));

			foreach (var serie in Series)
			{
				var pontosOrdenados = serie.Pontos.OrderBy(p => p.Data).ToList();
				if (pontosOrdenados.Count == 0) continue;

				if (pontosOrdenados.Count == 1)
				{
					var indice = todasAsDatas.IndexOf(pontosOrdenados[0].Data.Date);
					var centro = new PointF(IndiceParaX(indice), ValorParaY(pontosOrdenados[0].Valor));
					canvas.FillColor = serie.Cor;
					canvas.FillCircle(centro, 5);
					PontosDesenhados.Add(new PontoDesenhado(centro, serie, pontosOrdenados[0]));
					continue;
				}

				var caminho = new PathF();
				var centros = new List<PointF>();

				for (int i = 0; i < pontosOrdenados.Count; i++)
				{
					var indice = todasAsDatas.IndexOf(pontosOrdenados[i].Data.Date);
					var centro = new PointF(IndiceParaX(indice), ValorParaY(pontosOrdenados[i].Valor));
					centros.Add(centro);

					if (i == 0) caminho.MoveTo(centro);
					else caminho.LineTo(centro);
				}

				canvas.StrokeColor = serie.Cor;
				canvas.StrokeSize = 3;
				canvas.StrokeLineJoin = LineJoin.Round;
				canvas.StrokeLineCap = LineCap.Round;
				canvas.DrawPath(caminho);

				canvas.FillColor = serie.Cor;
				for (int i = 0; i < centros.Count; i++)
				{
					canvas.FillCircle(centros[i], 4);
					PontosDesenhados.Add(new PontoDesenhado(centros[i], serie, pontosOrdenados[i]));
				}
			}

			canvas.FontSize = 9;
			var outrasOpcoesGlobais = ObterOutrasOpcoes(Destaques);
			for (int f = 0; f < Destaques.Count; f++)
			{
				float yFaixa = padTopo + altura + 14 + (f * alturaPorFaixaDestaque);
				var destaque = Destaques[f];

				canvas.FontColor = CorRotulo;
				canvas.DrawString(destaque.Nome, 2, yFaixa - 7, dirtyRect.Width - padDireita, 14, HorizontalAlignment.Left, VerticalAlignment.Top);

				for (int i = 0; i < todasAsDatas.Count; i++)
				{
					float x = IndiceParaX(i);
					var temResposta = destaque.RespostasPorDia.TryGetValue(todasAsDatas[i], out var resposta);

					Color cor = !temResposta ? CorNaoRespondeu : CorParaResposta(resposta!, outrasOpcoesGlobais);

					canvas.FillColor = cor;
					canvas.FillCircle(x, yFaixa + 8, temResposta ? 4 : 3);

					if (!temResposta)
					{
						canvas.StrokeColor = cor;
						canvas.StrokeSize = 1;
						canvas.DrawCircle(x, yFaixa + 8, 5);
					}
				}
			}

			canvas.FontColor = CorRotulo;
			canvas.FontSize = 9;

			void DesenharRotuloData(int indice)
			{
				float x = IndiceParaX(indice);
				canvas.DrawString(todasAsDatas[indice].ToString("dd/MM"), x - 20, padTopo + altura + (Destaques.Count > 0 ? -4 : 6), 40, 14, HorizontalAlignment.Center, VerticalAlignment.Top);
			}

			DesenharRotuloData(0);
			if (todasAsDatas.Count > 2)
				DesenharRotuloData(todasAsDatas.Count / 2);
			if (todasAsDatas.Count > 1)
				DesenharRotuloData(todasAsDatas.Count - 1);
		}

		public PontoDesenhado? Hit(PointF toque, float raio = 16)
		{
			PontoDesenhado? maisProximo = null;
			double menorDistancia = double.MaxValue;

			foreach (var pd in PontosDesenhados)
			{
				var dx = pd.Centro.X - toque.X;
				var dy = pd.Centro.Y - toque.Y;
				var distancia = Math.Sqrt(dx * dx + dy * dy);

				if (distancia <= raio && distancia < menorDistancia)
				{
					menorDistancia = distancia;
					maisProximo = pd;
				}
			}

			return maisProximo;
		}
	}
}