using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Services;
using SilvaData.Utilities;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;

namespace SilvaData.ViewModels
{
    public partial class SincronizacaoViewModel : ViewModelBase
    {
        private readonly SyncService _syncService;
        private readonly CacheService _cacheService;

        [ObservableProperty]
        private string texto = Traducao.Aguarde;

        [ObservableProperty]
        private string texto2 = string.Empty;

        [ObservableProperty]
        private string progresso = string.Empty;

        [ObservableProperty]
        private string etapaAtual = string.Empty;

        [ObservableProperty]
        private int percent = 0;

        [ObservableProperty]
        private int subPercent = 0;

        [ObservableProperty]
        private string tempoDecorrido = "00s";

        [ObservableProperty]
        private bool indicadorVisivel = true;

        [ObservableProperty]
        private bool podeCancelar = true;

        [ObservableProperty]
        private bool cancelado = false;

        [ObservableProperty]
        private ObservableCollection<string> logs = new();

        [ObservableProperty]
        private string subTexto = string.Empty;

        [ObservableProperty]
        private bool mostrarLogs;

        // Timer pr�prio para o cron�metro de tempo decorrido � independente do CancellationToken
        // da sincroniza��o para que continue vis�vel mesmo durante popups de erro/retry.
        private CancellationTokenSource? _timerCts;

        public SincronizacaoViewModel(SyncService syncService, CacheService cacheService)
        {
            _syncService = syncService;
            _cacheService = cacheService;
        }

        [RelayCommand]
        private void ToggleLogs() => MostrarLogs = !MostrarLogs;

        private void StartTimer()
        {
            StopTimer();
            _timerCts = new CancellationTokenSource();
            var start = DateTime.UtcNow;

            _ = Task.Run(async () =>
            {
                try
                {
                    while (!_timerCts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(500, _timerCts.Token); // atualiza 2x por segundo
                        var elapsed = DateTime.UtcNow - start;
                        var tempo = elapsed.Minutes > 0
                            ? $"{elapsed.Minutes:00}m {elapsed.Seconds:00}s"
                            : $"{elapsed.Seconds:00}s";

                        // Atualiza apenas se necess�rio (evita marshaling desnecess�rio)
                        if (tempo != TempoDecorrido)
                        {
                            MainThread.BeginInvokeOnMainThread(() => TempoDecorrido = tempo);
                        }
                    }
                }
                catch (OperationCanceledException) { /* normal */ }
                catch (Exception ex) { Debug.WriteLine($"[Timer] Erro: {ex}"); }
            });
        }

        private void StopTimer()
        {
            _timerCts?.Cancel();
            _timerCts?.Dispose();
            _timerCts = null;
        }

        [RelayCommand(CanExecute = nameof(CanExecuteSync))]
        public async Task IniciaSincronizacao()
        {
            // ? Inicia antes de tudo
            StartTimer();

            Cancelado = false;
            PodeCancelar = true;
            Logs.Clear();
            IsBusy = true;

            IniciaSincronizacaoCommand.NotifyCanExecuteChanged();

            var progressReporter = new Progress<SyncProgressReport>(report =>
            {
                Texto = report.Texto;
                Texto2 = report.Texto2;
                EtapaAtual = report.Texto;
                SubTexto = report.SubTexto;

                // Progresso principal
                if (report.ProgressoTotal > 0)
                {
                    Progresso = $"{report.ProgressoAtual} / {report.ProgressoTotal}";
                    Percent = Math.Max(0, Math.Min(100, (int)Math.Round(100.0 * report.ProgressoAtual / report.ProgressoTotal)));
                }

                // Sub-progresso
                if (report.SubAtual.HasValue && report.SubTotal.HasValue && report.SubTotal.Value > 0)
                    SubPercent = Math.Max(0, Math.Min(100, (int)Math.Round(100.0 * report.SubAtual.Value / report.SubTotal.Value)));
                else
                    SubPercent = 0;

                if (Logs.Count > 100) Logs.RemoveAt(0);
                Logs.Add($"{DateTime.Now:HH:mm:ss} - {report.Texto} {report.Texto2}".Trim());
            });

            bool repeat = true;
            while (repeat)
            {
                string resultado = string.Empty;
                try
                {
                    IndicadorVisivel = true;
                    resultado = await _syncService.DownloadDataFromServer(progressReporter);
                    IndicadorVisivel = false;

                    if (!string.IsNullOrEmpty(resultado))
                        throw new Exception(resultado);

                    repeat = false;
                }
                catch (Exception e)
                {
                    if (Cancelado)
                    {
                        await PopUpOK.ShowAsync(Traducao.Atencao, Traducao.Opera��oCancelada);
                        repeat = false;
                        break;
                    }

                    Debug.WriteLine($"[Sync] Erro: {e.Message}");
                    bool querTentarNovamente = await PopUpYesNo.ShowAsync(
                        Traducao.Erro,
                        $"{Traducao.ASincroniza��oFalhouComAMensagem}\n\n'{e.Message}'\n\n{Traducao.DesejaTentarNovamente}",
                        Traducao.Sim,
                        Traducao.N�o);

                    if (querTentarNovamente)
                    {
                        var temInternet = Connectivity.NetworkAccess == NetworkAccess.Internet;
                        if (!temInternet)
                        {
                            bool continueChecking = true;
                            while (continueChecking && !temInternet)
                            {
                                if (!await PopUpYesNo.ShowAsync(Traducao.SemInternet, Traducao.ConectadoAInternet, Traducao.Sim, Traducao.N�o))
                                    continueChecking = false;
                                await Task.Delay(2000);
                                temInternet = Connectivity.NetworkAccess == NetworkAccess.Internet;
                            }
                            if (!temInternet) repeat = false;
                        }
                    }
                    else
                    {
                        repeat = false;
                    }
                }
            }

            // ? Garante que o timer � parado SEMPRE no fim
            StopTimer();

            IsBusy = false;
            PodeCancelar = false;
            IniciaSincronizacaoCommand.NotifyCanExecuteChanged();

            if (!Cancelado)
            {
                // Mostra spinner de "finalizando" enquanto o trabalho p�s-sync roda,
                // para que o popup de sucesso apare�a s� quando tudo estiver pronto
                // e o OK feche a tela sem delay percept�vel.
                EtapaAtual = "Sincroniza��o conclu�da!";
                SubTexto = "Atualizando dados locais, aguarde...";
                IndicadorVisivel = true;

                await Notificacao.CreateNotificationsAsync();
                await _cacheService.Refresh();
                WeakReferenceMessenger.Default.Send(new UpdateDadosIniciaisMessage());
                Permissoes.ChecaPermissoes();
                await AtualizarViewModelsDasTabs();

                IndicadorVisivel = false;
                SubTexto = string.Empty;

                await PopUpOK.ShowAsync(Traducao.Sucesso, Traducao.DadosRecebidosComSucesso);

                EtapaAtual = "Abrindo tela principal...";
                SubTexto = string.Empty;
                IndicadorVisivel = true;
            }

            if (NavigationUtils.IsModalDisplayed(typeof(SincronizacaoPageModal)))
                await NavigationUtils.PopModalAsync();
        }

        [RelayCommand(CanExecute = nameof(CanExecuteCancelar))]
        private void Cancelar()
        {
            if (!PodeCancelar) return;
            Cancelado = true;
            _syncService.Cancel();
            PodeCancelar = false;
            Logs.Add("Cancelamento solicitado.");
        }

        private bool CanExecuteSync() => !IsBusy;
        private bool CanExecuteCancelar() => PodeCancelar && !Cancelado;

        private async Task AtualizarViewModelsDasTabs()
        {
            try
            {
                var loteViewModel = ServiceHelper.GetRequiredService<LoteViewModel>();
                var homeViewModel = ServiceHelper.GetRequiredService<HomeViewModel>();

                await loteViewModel.LimparFiltros();
                await loteViewModel.CarregaLotes();

                await homeViewModel.CarregaDados(true);
                MainThread.BeginInvokeOnMainThread(homeViewModel.AtualizaUI);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SincronizacaoVM] Erro ao atualizar tabs: {ex.Message}");
            }
        }
    }
}
