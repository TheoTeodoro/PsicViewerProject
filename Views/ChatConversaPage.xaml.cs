using MauiApp1.ViewModels;

namespace MauiApp1.Views;

public partial class ChatConversaPage : ContentPage
{
	private readonly ChatConversaViewModel _viewModel;

	public ChatConversaPage(ChatConversaViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = viewModel;
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();

		// OnDisappearing também dispara quando uma modal (como a de
		// visualizar imagem) só COBRE essa tela por cima — não quando a
		// conversa realmente fecha. Se essa página ainda está na pilha
		// de navegação, foi só coberta; só limpa de verdade (parar o
		// áudio, desinscrever eventos) quando ela foi de fato removida
		// por um Pop. Sem essa checagem, abrir uma foto derrubava o
		// player de áudio no meio da reprodução (crash) e desligava as
		// mensagens em tempo real até sair e entrar de novo no Chat.
		if (!Navigation.NavigationStack.Contains(this))
		{
			_viewModel.Dispose();
		}
	}
}