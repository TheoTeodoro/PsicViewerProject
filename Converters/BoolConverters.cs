using System.Globalization;

namespace MauiApp1.Converters
{
	/// <summary>true (enviada por mim) -> alinha à direita; false -> à esquerda.</summary>
	public class BoolParaAlinhamentoConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> (value is true) ? LayoutOptions.End : LayoutOptions.Start;

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}

	/// <summary>true (minha mensagem) -> bolha azul; false -> bolha cinza clara.</summary>
	public class BoolParaCorConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> (value is true) ? Color.FromArgb("#004AAD") : Color.FromArgb("#EEEEEE");

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}

	/// <summary>true (minha mensagem, fundo azul) -> texto branco; false -> texto escuro.</summary>
	public class BoolParaTextoConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> (value is true) ? Colors.White : Color.FromArgb("#1C274C");

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}

	/// <summary>Inverte um bool — usado pra alternar entre dois elementos
	/// (ex: mostrar o botão de microfone quando NÃO está gravando).</summary>
	public class InversoBoolConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> !(value is true);

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> !(value is true);
	}

	/// <summary>true se a string NÃO está vazia — usado pra mostrar/esconder
	/// banners de erro só quando existe mensagem pra exibir.</summary>
	public class StringNaoVazioConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> !string.IsNullOrWhiteSpace(value as string);

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}

	/// <summary>Notificação não lida -> texto em negrito; lida -> normal.</summary>
	public class NaoLidaParaFonteConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> (value is true) ? FontAttributes.Bold : FontAttributes.None;

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}

	/// <summary>Contador > 0 -> mostra o badge do sino; 0 -> esconde.</summary>
	public class ContadorParaVisivelConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value is int i && i > 0;

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}

	/// <summary>Abas de filtro (Todos/Ativos/Arquivados) — a aba selecionada
	/// fica branca, as outras cinza claro.</summary>
	public class FiltroParaCorConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> (value as string) == (parameter as string) ? Colors.White : Color.FromArgb("#F0F0F0");

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}

	/// <summary>Seletor de Tipo de Pergunta (Escala/Texto/Múltipla Escolha)
	/// — a opção selecionada vira uma pílula azul preenchida.</summary>
	public class TipoPerguntaCorFundoConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> (value as string) == (parameter as string) ? Color.FromArgb("#004AAD") : Colors.Transparent;

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}

	public class TipoPerguntaCorTextoConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> (value as string) == (parameter as string) ? Colors.White : Color.FromArgb("#8A94A6");

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}

	/// <summary>Liga o TimePicker (que trabalha com TimeSpan) a uma
	/// propriedade de texto "HH:mm" no ViewModel — os dois sentidos são
	/// usados de verdade aqui (binding é TwoWay).</summary>
	public class StringParaTimeSpanConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> TimeSpan.TryParse(value as string, out var t) ? t : new TimeSpan(8, 0, 0);

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value is TimeSpan t ? t.ToString(@"hh\:mm") : "08:00";
	}

	/// <summary>Opacidade cheia pro emoji/opção selecionada, meio-apagado
	/// pros demais — usado na tela de Responder Questionário.</summary>
	public class ValorIgualParaOpacidadeConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			var valorTexto = value?.ToString();
			var parametroTexto = parameter?.ToString();
			return valorTexto == parametroTexto ? 1.0 : 0.4;
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}

	/// <summary>A opção de escala selecionada fica ~40% maior que as
	/// demais (Scale=1.4 vs 1.0) — a "elevação" visual pedida.</summary>
	public class ValorIgualParaEscalaConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			var valorTexto = value?.ToString();
			var parametroTexto = parameter?.ToString();
			return valorTexto == parametroTexto ? 1.4 : 1.0;
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}

	/// <summary>Igual ao de cima, mas comparando dois valores vindos de
	/// bindings (não um valor fixo) — usado nas opções de Múltipla
	/// Escolha, onde cada item da lista precisa comparar consigo mesmo
	/// contra a opção selecionada no ViewModel.</summary>
	public class DoisValoresIguaisParaOpacidadeConverter : IMultiValueConverter
	{
		public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
		{
			if (values.Length < 2) return 1.0;
			var a = values[0]?.ToString();
			var b = values[1]?.ToString();
			return a == b ? 1.0 : 0.4;
		}

		public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}

	/// <summary>Transforma uma fração de progresso (0 a 1) numa posição em
	/// pixels — usado pra deslizar o pontinho na barrinha de progresso do
	/// áudio. O parâmetro é a largura (em pixels) da barrinha.</summary>
	public class ProgressoParaPosicaoConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			var progresso = value is double d ? d : 0;
			var largura = parameter is string s && double.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, out var w) ? w : 140;
			return progresso * largura;
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}
}