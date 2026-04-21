using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utilities;

using HapticHelper = global::SilvaData.Utilities.HapticHelper;


namespace SilvaData.ViewModels
{
    /// <summary>
    /// ViewModel para a página de edição ou criação de um Proprietário.
    /// MIGRADO: Usa Messaging para comunicação com a View (sem acoplamento direto).
    /// </summary>
    public partial class ProprietarioEditViewModel : BaseEditViewModel
    {
        private readonly CacheService _cacheService;

        [ObservableProperty]
        private Proprietario proprietario = new();

        // REMOVIDO: internal static Action<int?>? EventoAoAdicionar = null;
        // Agora usa WeakReferenceMessenger para notificar a adição

        public ProprietarioEditViewModel(CacheService cacheService)
        {
            _cacheService = cacheService;
        }

        /// <summary>
        /// Define o estado inicial do ViewModel com base no Proprietário fornecido.
        /// MIGRADO: Não recebe mais IValidatablePage (a View se registra sozinha).
        /// </summary>
        /// <param name="proprietario">O Proprietário a ser editado, ou null para criar um novo.</param>
        public void SetInitialState(Proprietario? proprietario)
        {
            _hasLoaded = false;
            if (proprietario == null)
            {
                NovoRegistro = true;
                Proprietario = new Proprietario { id = 0 };
                Title = Traducao.NovoProprietário;
                SubTitle = Traducao.AdicioneAsInformaçõesAbaixo;
            }
            else
            {
                NovoRegistro = false;
                Proprietario = proprietario;
                Title = Traducao.EditarProprietário;
                SubTitle = Traducao.AdicioneAsInformaçõesAbaixo;
            }
        }

        /// <summary>
        /// Carrega os dados necessários para a página.
        /// (Nenhum dado assíncrono é necessário para esta entidade simples).
        /// </summary>
        public override Task GetItemOrCreateANew()
        {
            // Proprietário é uma entidade simples, sem dependências
            return Task.CompletedTask;
        }

        /// <summary>
        /// MIGRADO: Usa ValidateViewAsync() (que envia mensagem) ao invés de View.ValidateFormAsync()
        /// </summary>
        public override async Task SaveAndReturn()
        {
            if (IsBusy) return;

            // 1. Aciona a validação da UI via Messenger
            if (!await ValidateViewAsync())
            {
                return; // A validação falhou
            }

            // 2. Validação passou, continua com o save
            try
            {
                IsBusy = true;
                DataSaved = true;

                Proprietario.excluido = 0;
                await Proprietario.SaveItemAsync(Proprietario);

                string popupMessage;
                if (NovoRegistro)
                {
                    popupMessage = Traducao.ProprietárioAdicionadoComSucesso;

                    // MUDANÇA: Usa Messaging ao invés de evento estático
                    WeakReferenceMessenger.Default.Send(new ProprietarioAdicionadoMessage(Proprietario));
                }
                else
                {
                    popupMessage = Traducao.ProprietárioAlteradoComSucesso;

                    WeakReferenceMessenger.Default.Send(new ProprietarioSalvoMessage(Proprietario));
                }

                HapticHelper.VibrateClick();
                await PopUpOK.ShowAsync(Title, popupMessage);

                // 3. Pede à View para fechar via Messenger
                WeakReferenceMessenger.Default.Send(new ClosePageRequestMessage());
            }
            catch (Exception ex)
            {
                await PopUpOK.ShowAsync(Traducao.Erro, $"Erro ao salvar: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
