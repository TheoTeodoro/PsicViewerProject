using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Services;

namespace MauiApp1.ViewModels
{
	public partial class EditarPerfilViewModel : ObservableObject
	{
		private readonly ContaApiService _conta;
		private readonly ArquivoUploadService _upload;
		private readonly SessaoUsuario _sessao;

		[ObservableProperty]
		private string nome = string.Empty;

		[ObservableProperty]
		private string email = string.Empty;

		[ObservableProperty]
		private string crp = string.Empty;

		[ObservableProperty]
		private string telefone = string.Empty;

		[ObservableProperty]
		private DateTime dataNascimento = DateTime.Today.AddYears(-18);

		[ObservableProperty]
		private string generoSelecionado = string.Empty;

		[ObservableProperty]
		private string? fotoUrl;

		[ObservableProperty]
		private bool ehPsicologo;

		[ObservableProperty]
		private string mensagemErro = string.Empty;

		[ObservableProperty]
		private bool carregando;

		public string FotoExibida => string.IsNullOrEmpty(FotoUrl)
			? "avatar_placeholder.jpg"
			: $"{ApiConfig.ServidorBaseUrl}{FotoUrl}";

		public string[] OpcoesGenero => GeneroHelper.Opcoes;

		partial void OnFotoUrlChanged(string? value) => OnPropertyChanged(nameof(FotoExibida));

		public EditarPerfilViewModel(ContaApiService conta, ArquivoUploadService upload, SessaoUsuario sessao)
		{
			_conta = conta;
			_upload = upload;
			_sessao = sessao;
		}

		public async Task CarregarAsync()
		{
			Carregando = true;
			MensagemErro = string.Empty;
			try
			{
				EhPsicologo = _sessao.Tipo == TipoUsuarioLogado.Psicologo;

				var usuario = EhPsicologo
					? await _conta.ObterPsicologoAsync(_sessao.UsuarioId)
					: await _conta.ObterPacienteAsync(_sessao.UsuarioId);

				if (usuario is null)
				{
					MensagemErro = "Não foi possível carregar seus dados.";
					return;
				}

				Nome = usuario.Nome;
				Email = usuario.Email;
				Crp = usuario.Crp ?? string.Empty;
				Telefone = usuario.Telefone ?? string.Empty;
				DataNascimento = usuario.DataNascimento ?? DateTime.Today.AddYears(-18);
				GeneroSelecionado = GeneroHelper.ParaExibicao(usuario.Genero);
				FotoUrl = usuario.FotoUrl;
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível conectar ao servidor: " + ex.Message;
			}
			finally
			{
				Carregando = false;
			}
		}

		[RelayCommand]
		private async Task EscolherFotoAsync()
		{
			var escolha = await Application.Current!.MainPage!.DisplayActionSheet(
				"Foto de perfil", "Cancelar", null, "Tirar foto", "Escolher da galeria");

			try
			{
				FileResult? foto = null;

				if (escolha == "Tirar foto")
				{
					var statusCamera = await Permissions.RequestAsync<Permissions.Camera>();
					if (statusCamera != PermissionStatus.Granted)
					{
						MensagemErro = "Permissão de câmera negada.";
						return;
					}

					if (MediaPicker.Default.IsCaptureSupported)
						foto = await MediaPicker.Default.CapturePhotoAsync();
				}
				else if (escolha == "Escolher da galeria")
				{
					foto = await MediaPicker.Default.PickPhotoAsync();
				}

				if (foto is null) return;

				Carregando = true;
				var (caminhoServidor, _) = await _upload.EnviarAsync(foto.FullPath, foto.FileName);
				FotoUrl = caminhoServidor;
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível atualizar a foto: " + ex.Message;
			}
			finally
			{
				Carregando = false;
			}
		}

		[RelayCommand]
		private async Task SalvarAsync()
		{
			Carregando = true;
			MensagemErro = string.Empty;
			try
			{
				var (sucesso, erro) = EhPsicologo
					? await _conta.AtualizarPsicologoAsync(_sessao.UsuarioId, Nome, Email, Telefone, DataNascimento, GeneroHelper.ParaValorApi(GeneroSelecionado), FotoUrl, Crp)
					: await _conta.AtualizarPacienteAsync(_sessao.UsuarioId, Nome, Email, Telefone, DataNascimento, GeneroHelper.ParaValorApi(GeneroSelecionado), FotoUrl);

				if (!sucesso)
				{
					MensagemErro = erro ?? "Não foi possível salvar.";
					return;
				}

				_sessao.AtualizarDadosBasicos(Nome, Email, FotoUrl);

				await Application.Current!.MainPage!.Navigation.PopAsync();
			}
			catch (Exception ex)
			{
				MensagemErro = "Não foi possível conectar ao servidor: " + ex.Message;
			}
			finally
			{
				Carregando = false;
			}
		}
	}
}
