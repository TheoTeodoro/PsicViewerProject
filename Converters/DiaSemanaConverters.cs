using System.Globalization;

namespace MauiApp1.Converters
{
	/// <summary>Fundo do círculo de dia da semana: azul quando marcado,
	/// cinza claro (mesmo tom neutro usado no campo de busca) quando não.</summary>
	public class DiaAtivoFundoConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			var ativo = value is bool b && b;
			if (ativo && Application.Current?.Resources.TryGetValue("AzulPrimario", out var cor) == true)
				return cor;
			return Color.FromArgb("#ECEBF3");
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}

	/// <summary>Texto do círculo de dia da semana: branco quando marcado,
	/// cinza médio quando não.</summary>
	public class DiaAtivoTextoConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			var ativo = value is bool b && b;
			return ativo ? Colors.White : Color.FromArgb("#9B9C96");
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}
}