using System.Globalization;

namespace MauiApp1.Converters
{
	/// <summary>Compara o valor vinculado (string) com o ConverterParameter
	/// — usado pra mostrar/esconder algo só quando o filtro selecionado
	/// bate com um valor específico (ex: alternar entre as listas de
	/// "Pendentes" e "Histórico").</summary>
	public class StringIgualConverter : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> value is string valor && parameter is string esperado && valor == esperado;

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}
}