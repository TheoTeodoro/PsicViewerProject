namespace MauiApp1.Services
{
	public static class FrequenciaHelper
	{
		public static readonly string[] Opcoes = { "Todos os dias", "Dias úteis", "Semanalmente" };

		public static string ParaValorApi(string? opcaoExibida) => opcaoExibida switch
		{
			"Dias úteis" => "DiasUteis",
			"Semanalmente" => "Semanalmente",
			_ => "TodosOsDias"
		};

		public static string ParaExibicao(string? valorApi) => valorApi switch
		{
			"DiasUteis" => "Dias úteis",
			"Semanalmente" => "Semanalmente",
			_ => "Todos os dias"
		};
	}
}