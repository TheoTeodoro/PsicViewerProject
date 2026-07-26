namespace MauiApp1.Services
{
	/// <summary>
	/// Único lugar com o endereço do servidor. ChatConnectionService e
	/// ArquivoUploadService leem daqui — assim, se o IP mudar, só precisa
	/// editar em um lugar (antes tínhamos isso duplicado, e foi exatamente
	/// aí que ficou desatualizado numa das rodadas de teste).
	///
	/// IMPORTANTE: o IP da sua máquina pode mudar quando o roteador
	/// reatribui endereços (reinício do roteador, do PC, etc). Se o app
	/// parar de conectar do nada, roda "ipconfig" no CMD de novo e
	/// atualiza esse valor.
	/// </summary>
	public static class ApiConfig
	{
		public const string ServidorBaseUrl = "http://192.168.0.108:5299";
	}
}