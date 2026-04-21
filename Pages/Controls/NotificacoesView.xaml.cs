using CommunityToolkit.Mvvm.Input;
// Custom Usings (Migrated)
using SilvaData.Models;
using SilvaData.Utils; // For ContentPageWithLocalization
using SilvaData.Pages.PopUps; // For PopUpYesNo
using SilvaData.Utilities; // For NavigationUtils

using System.Collections.ObjectModel;

namespace ISIInstitute.Views
{
    public partial class NotificacoesView : ContentPageWithLocalization
    {
        private ObservableCollection<Notificacao> _notificacoes = new();

        public ObservableCollection<Notificacao> Notificacoes
        {
            get => _notificacoes;
            set
            {
                if (_notificacoes == value) return;
                _notificacoes = value ?? new ObservableCollection<Notificacao>();
                OnPropertyChanged(nameof(Notificacoes));
                OnPropertyChanged(nameof(TemNotificacao));
                OnPropertyChanged(nameof(NaoTemNotificacao));
            }
        }

        public bool TemNotificacao => Notificacoes?.Count > 0;
        public bool NaoTemNotificacao => !TemNotificacao;

        public NotificacoesView()
        {
            InitializeComponent();
            BindingContext = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = OnAppearingInternalAsync();
        }

        private async Task OnAppearingInternalAsync()
        {
            try
            {
                var busy = this.FindByName<LoadingView>("busyindicator");
                if (busy != null)
                {
                    busy.IsVisible = true;
                    busy.Text = Traducao.CarregandoAtividades;
                }

                var ativas = (await Notificacao.PegaNotificacoesAtivas())
                    .Where(n => n.dataHora <= DateTime.Now);

                Notificacoes = new ObservableCollection<Notificacao>(ativas);

                if (busy != null) busy.IsVisible = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificacoesView] Erro em OnAppearing: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task RemoveNotificacao(object notificacaoSelecionada)
        {
            if (notificacaoSelecionada is Notificacao notificacao)
            {
                if (await PopUpYesNo.ShowAsync(Traducao.NotificacoesView_ArquivarNotifica��o, Traducao.ArquivarNotificacaoDesc, Traducao.Sim, Traducao.Nao))
                {
                    var busy = this.FindByName<LoadingView>("busyindicator");
                    if (busy != null) busy.IsVisible = true;

                    notificacao.dataHoraArquivado = DateTime.Now;
                    await Notificacao.SalvaNotificacao(notificacao);

                    // Remove local
                    Notificacoes.Remove(notificacao);

                    // Atualiza contadores dependentes
                    await Notificacao.PegaNotificacoesAtivas();

                    if (busy != null) busy.IsVisible = false;

                    OnPropertyChanged(nameof(TemNotificacao));
                    OnPropertyChanged(nameof(NaoTemNotificacao));
                }
            }
        }

        [RelayCommand]
        public async Task Voltar()
        {
            await NavigationUtils.PopModalAsync();
        }
    }
}
