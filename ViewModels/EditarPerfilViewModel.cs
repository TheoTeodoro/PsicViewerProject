using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PsicViewer.Core.Interfaces;
using MauiApp1.Services;

namespace MauiApp1.ViewModels
{
	public partial class EditarPerfilViewModel : ObservableObject
	{
		private readonly IPacienteRepository _pacientes;
		private readonly IPsicologoRepository _psicologos;
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
		private string? fotoUrl;

		[ObservableProperty]
		private bool ehPsicologo;

		[ObservableProperty]
		private string mensagemErro = string.Empty;

		[ObservableProperty]
		private bool carregando;

		/// <summary>Caminho relativo (ex: "/arquivos/x.jpg") -> URL completa
		/// pra exibir, ou o placeholder local se ainda não tem foto.</summary>
		public string FotoExibida => string.IsNullOrEmpty(FotoUrl)
			? "avatar_placeholder.jpg"
			: $"{ApiConfig.ServidorBaseUrl}{FotoUrl}";

		partial void OnFotoUrlChanged(string? value) => OnPropertyChanged(nameof(FotoExibida));

		public EditarPerfilViewModel(
			IPacienteRepository pacientes,
			IPsicologoRepository psicologos,
			ArquivoUploadService upload,
			SessaoUsuario sessao)
		{
			_pacientes = pacientes;
			_psicologos = psicologos;
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

				if (EhPsicologo)
				{
					var p = await _psicologos.ObterPorIdAsync(_sessao.UsuarioId);
					if (p is null) { MensagemErro = "Não foi possível carregar seus dados."; return; }

					Nome = p.Nome;
					Email = p.Email;
					Crp = p.Crp;
					Telefone = p.Telefone ?? string.Empty;
					DataNascimento = p.DataNascimento ?? DateTime.Today.AddYears(-18);
					FotoUrl = p.FotoUrl;
				}
				else
				{
					var p = await _pacientes.ObterPorIdAsync(_sessao.UsuarioId);
					if (p is null) { MensagemErro = "Não foi possível carregar seus dados."; return; }

					Nome = p.Nome;
					Email = p.Email;
					Telefone = p.Telefone ?? string.Empty;
					DataNascimento = p.DataNascimento ?? DateTime.Today.AddYears(-18);
					FotoUrl = p.FotoUrl;
				}
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
				if (EhPsicologo)
				{
					var p = await _psicologos.ObterPorIdAsync(_sessao.UsuarioId);
					if (p is null) { MensagemErro = "Usuário não encontrado."; return; }

					p.AtualizarDados(Nome, Email, Telefone, DataNascimento, Crp);
					p.AtualizarFoto(FotoUrl);
					await _psicologos.AtualizarAsync(p);
				}
				else
				{
					var p = await _pacientes.ObterPorIdAsync(_sessao.UsuarioId);
					if (p is null) { MensagemErro = "Usuário não encontrado."; return; }

					p.AtualizarDados(Nome, Email, Telefone, DataNascimento);
					p.AtualizarFoto(FotoUrl);
					await _pacientes.AtualizarAsync(p);
				}

				_sessao.AtualizarDadosBasicos(Nome, Email, FotoUrl);

				await Application.Current!.MainPage!.Navigation.PopAsync();
			}
			catch (ArgumentException ex)
			{
				MensagemErro = ex.Message;
			}
			catch (Exception ex)
			{
				MensagemErro = "Erro inesperado: " + ex.Message;
			}
			finally
			{
				Carregando = false;
			}
		}
	}
}