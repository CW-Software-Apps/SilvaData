using SilvaData.ViewModels;
using SilvaData.Utilities;

namespace ISIInstitute.Views.LoteViews;

/// <summary>
/// Página de monitoramento do lote com opções de acesso aos módulos específicos.
/// Criada por abertura para evitar reuso de visual tree em modal iOS.
/// O estado inicial continua sendo configurado no ViewModel antes da navegação.
/// </summary>
public partial class LoteMonitoramentoView : ContentPage
{
    private readonly LoteMonitoramentoViewModel _viewModel;
    private bool _hasAppearedOnce;

    public LoteMonitoramentoView()
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetRequiredService<LoteMonitoramentoViewModel>();
        BindingContext = _viewModel;
    }

    /// <summary>
    /// Chamado a cada abertura do modal. SetInitialState já foi chamado pelo caller.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = OnAppearingInternalAsync();
    }

    private async Task OnAppearingInternalAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[LoteMonitoramentoView.OnAppearing] _hasAppearedOnce={_hasAppearedOnce}");

            // Carrega dados em paralelo com a animação (não bloqueia um no outro)
            var loadTask = _viewModel.LoadDataAfterAppear();

            if (!_hasAppearedOnce)
            {
                _hasAppearedOnce = true;
                System.Diagnostics.Debug.WriteLine($"[LoteMonitoramentoView.OnAppearing] Executando RunEntranceAnimationAsync");
                await RunEntranceAnimationAsync();
            }

            System.Diagnostics.Debug.WriteLine($"[LoteMonitoramentoView.OnAppearing] Aguardando LoadDataAfterAppear");
            await loadTask;
            System.Diagnostics.Debug.WriteLine($"[LoteMonitoramentoView.OnAppearing] LoadDataAfterAppear concluído");

            await _viewModel.GetItemOrCreateANew();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoteMonitoramentoView] Erro em OnAppearing: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private async Task RunEntranceAnimationAsync()
    {
        if (DeviceInfo.Platform == DevicePlatform.iOS)
        {
            HeaderGrid.Opacity = 1;
            HeaderGrid.TranslationY = 0;
            ContentStack.Opacity = 1;
            ContentStack.TranslationY = 0;
            return;
        }

        await MainScrollView.ScrollToAsync(0, 0, false);
        HeaderGrid.Opacity = 0;
        HeaderGrid.TranslationY = -25;
        ContentStack.Opacity = 0;
        ContentStack.TranslationY = 35;

        // Header desce do topo
        var headerIn = Task.WhenAll(
            HeaderGrid.FadeToAsync(1, 220, Easing.CubicOut),
            HeaderGrid.TranslateToAsync(0, 0, 220, Easing.CubicOut));

        // Pequeno delay para o conteúdo entrar logo depois
        await Task.Delay(80);

        var contentIn = Task.WhenAll(
            ContentStack.FadeToAsync(1, 280, Easing.CubicOut),
            ContentStack.TranslateToAsync(0, 0, 280, Easing.CubicOut));

        await Task.WhenAll(headerIn, contentIn);
    }

    private void OnParametroGalpaoTapped(object sender, TappedEventArgs e)
    {
        if (sender is not Border border || border.BindingContext is not ParametroGalpaoResumo resumo) return;

        _ = Task.Run(async () =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await border.FadeTo(0.4, 80);
                await border.FadeTo(1.0, 80);
            });
            await _viewModel.VaiParaAvaliacaoGalpaoCommand.ExecuteAsync(resumo);
        });
    }

    /// <summary>
    /// Trata o botão de voltar do dispositivo.
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        _ = NavigationUtils.PopModalAsync();
        return true;
    }
}
