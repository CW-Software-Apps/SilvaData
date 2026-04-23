using SilvaData.Models;
using SilvaData.ViewModels;

using Microsoft.Maui;
using Microsoft.Maui.Controls;

using System.Diagnostics;

namespace SilvaData.Controls
{
    public partial class LoteISIMacroView : ContentPage
    {
        private readonly LoteISIMacroViewModel _viewModel;
        private readonly Stopwatch _constructionStopwatch;
        private Lote? _lote;
        private int? _loadedLoteId;
        private bool _pendingReload;

        public LoteISIMacroView()
        {
            _constructionStopwatch = Stopwatch.StartNew();
            NavigationUtils.LogExternal(nameof(LoteISIMacroView), "Construtor iniciado");

            var initializeStopwatch = Stopwatch.StartNew();
            InitializeComponent();
            initializeStopwatch.Stop();
            NavigationUtils.LogExternal(nameof(LoteISIMacroView), $"InitializeComponent concluido em {initializeStopwatch.ElapsedMilliseconds}ms");

            _viewModel = ServiceHelper.GetRequiredService<LoteISIMacroViewModel>();
            BindingContext = _viewModel;

            // Evita competir com a animação de modal no iOS.
            this.Opacity = DeviceInfo.Platform == DevicePlatform.iOS ? 1 : 0;

            _constructionStopwatch.Stop();
            NavigationUtils.LogExternal(nameof(LoteISIMacroView), $"Construtor concluido em {_constructionStopwatch.ElapsedMilliseconds}ms | platform={DeviceInfo.Platform}");
        }

        public void SetInitialState(Lote lote)
        {
            ArgumentNullException.ThrowIfNull(lote);

            _lote = lote;
            _viewModel.Lote = lote;
            _pendingReload = true;
            _loadedLoteId = null;

            NavigationUtils.LogExternal(nameof(LoteISIMacroView), $"SetInitialState | lote={lote.id}");
        }


        private bool _isFirstAppearance = true;
        private int _previousIsiMacroCount = 0;

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _ = OnAppearingInternalAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(LoteISIMacroViewModel.IsiMacroList)) return;

            var newCount = _viewModel.IsiMacroList.Count;
            var wasNonEmpty = _previousIsiMacroCount > 0;
            var itemAdded = newCount > _previousIsiMacroCount;
            _previousIsiMacroCount = newCount;

            if (itemAdded && wasNonEmpty)
                _ = AnimateNewItemAsync();
        }

        private async Task AnimateNewItemAsync()
        {
            try
            {
                // Dá tempo pro SfListView renderizar o novo item
                await Task.Delay(150);

                // Scroll suave para o topo onde está o item mais recente
                isiMacroListView.ScrollTo(0, Microsoft.Maui.Controls.ScrollToPosition.Start, true);

                await Task.Delay(300);

                // Animação de "bounce" suave no container da lista
                await isiMacroListView.ScaleTo(1.015, 120, Easing.CubicOut);
                await isiMacroListView.ScaleTo(1.0, 200, Easing.SpringOut);
            }
            catch (Exception ex)
            {
                NavigationUtils.LogExternal(nameof(LoteISIMacroView), $"AnimateNewItem erro: {ex.Message}");
            }
        }

        private async Task OnAppearingInternalAsync()
        {
            var appearingStopwatch = Stopwatch.StartNew();
            try
            {
                NavigationUtils.LogExternal(nameof(LoteISIMacroView), $"OnAppearingInternalAsync iniciado | firstAppearance={_isFirstAppearance} | pendingReload={_pendingReload} | lote={_lote?.id}");

                if (DeviceInfo.Platform != DevicePlatform.iOS)
                {
                    await this.FadeToAsync(1, 300, Easing.CubicInOut);
                }

                if (_lote == null)
                {
                    NavigationUtils.LogExternal(nameof(LoteISIMacroView), "OnAppearingInternalAsync sem lote definido - ignorando carga");
                    return;
                }

                if (_isFirstAppearance || _pendingReload || _loadedLoteId != _lote.id)
                {
                    _isFirstAppearance = false;
                    _pendingReload = false;
                    _loadedLoteId = _lote.id;

                    // No iOS, OnAppearing dispara DURANTE a animação do modal.
                    // Aguarda a animação terminar (~350ms) antes de tocar no SQLite,
                    // evitando o deadlock no GCD thread pool.
                    if (DeviceInfo.Platform == DevicePlatform.iOS)
                        await Task.Delay(500);

                    await _viewModel.CarregaDados(_lote);
                }
            }
            catch (Exception ex)
            {
                NavigationUtils.LogExternal(nameof(LoteISIMacroView), $"Erro em OnAppearing: {ex.Message}");
            }
            finally
            {
                appearingStopwatch.Stop();
                NavigationUtils.LogExternal(nameof(LoteISIMacroView), $"OnAppearingInternalAsync concluido em {appearingStopwatch.ElapsedMilliseconds}ms | firstAppearance={_isFirstAppearance} | pendingReload={_pendingReload} | lote={_lote?.id}");
            }
        }

        private void OnIsiMacroItemTapped(object sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
        {
            if (e.DataItem is not SilvaData.ViewModels.ISIMacroButton button) return;
            _viewModel.ShowISIMacroCommand.Execute(button);
            _ = ClearSelectionAsync();
        }

        private async Task ClearSelectionAsync()
        {
            await Task.Delay(300);
            isiMacroListView.SelectedItem = null;
        }

        protected override bool OnBackButtonPressed()
        {
            _ = NavigationUtils.PopModalAsync();
            return true;
        }
    }
}
