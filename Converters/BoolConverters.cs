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
}
