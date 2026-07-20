namespace MauiApp1.Services
{
	/// <summary>
	/// Único lugar com o endereço do servidor. ChatConnectionService e
	/// ArquivoUploadService leem daqui — assim, se o IP mudar, só precisa
	/// editar em um lugar (antes tínhamos isso duplicado, e foi exatamente
	/// aí que ficou desatualizado numa das rodadas de teste).
	/// </summary>
	public static class ApiConfig
	{
		// TODO: troque pelo IP da sua máquina na rede (ipconfig no CMD).
		public const string ServidorBaseUrl = "http://192.168.0.114:5299";
	}
}
