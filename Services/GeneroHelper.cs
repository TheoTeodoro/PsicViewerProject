namespace MauiApp1.Services
{
	/// <summary>Converte entre o texto bonito mostrado no Picker
	/// ("Prefiro não dizer") e o valor que a API entende (nome do enum
	/// GeneroUsuario no Core: "PrefiroNaoDizer").</summary>
	public static class GeneroHelper
	{
		public static readonly string[] Opcoes =
		{
			"Masculino",
			"Feminino",
			"Outro",
			"Prefiro não dizer"
		};

		public static string? ParaValorApi(string? opcaoExibida) => opcaoExibida switch
		{
			"Prefiro não dizer" => "PrefiroNaoDizer",
			null or "" => null,
			_ => opcaoExibida
		};

		public static string ParaExibicao(string? valorApi) => valorApi switch
		{
			"PrefiroNaoDizer" => "Prefiro não dizer",
			null or "" => string.Empty,
			_ => valorApi
		};
	}
}
