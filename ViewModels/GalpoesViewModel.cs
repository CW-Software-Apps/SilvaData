using CommunityToolkit.Mvvm.Messaging;

namespace SilvaData.ViewModels
{
    public partial class GalpoesViewModel : ViewModelBase
    {
        private readonly CacheService _cacheService;

        [ObservableProperty]
        private string _textoPesquisa = string.Empty;

        [ObservableProperty]
        private ObservableCollection<UnidadeEpidemiologicaComDetalhes> _listaFiltrada = [];

        public static bool PodeAdicionar => Permissoes.UsuarioPermissoes?.regionais.cadastrar ?? false;
        public static bool PodeEditar => Permissoes.UsuarioPermissoes?.regionais.atualizar ?? false;

        public int TotalGalpoes => _listaFiltrada.Count;

        public GalpoesViewModel(CacheService cacheService)
        {
            _cacheService = cacheService;
            AplicaFiltro();

            WeakReferenceMessenger.Default.Register<UEAdicionadaMessage>(this, (_, m) =>
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ListaFiltrada.Insert(0, (UnidadeEpidemiologicaComDetalhes)m.UnidadeEpidemiologica);
                    OnPropertyChanged(nameof(TotalGalpoes));
                }));

            WeakReferenceMessenger.Default.Register<UESalvaMessage>(this, (_, m) =>
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var idx = ListaFiltrada.IndexOf(ListaFiltrada.FirstOrDefault(u => u.id == m.UnidadeEpidemiologica.id)!);
                    if (idx >= 0)
                        ListaFiltrada[idx] = (UnidadeEpidemiologicaComDetalhes)m.UnidadeEpidemiologica;
                }));
        }

        partial void OnTextoPesquisaChanged(string _) => AplicaFiltro();

        public void AplicaFiltro()
        {
            var texto = TextoPesquisa?.Trim() ?? string.Empty;
            var fonte = _cacheService.UEList.AsEnumerable();

            if (!string.IsNullOrEmpty(texto))
            {
                var busca = LocalizationManager.RemoveDiacritics(texto.ToUpperInvariant());
                fonte = fonte.Where(u => u.nome != null &&
                    LocalizationManager.RemoveDiacritics(u.nome.ToUpperInvariant()).Contains(busca));
            }

            ListaFiltrada = new ObservableCollection<UnidadeEpidemiologicaComDetalhes>(fonte);
            OnPropertyChanged(nameof(TotalGalpoes));
        }

        [RelayCommand]
        private static async Task AdicionarGalpao()
        {
            await NavigationUtils.ShowViewAsModalAsync<UnidadeEpidemiologicaView_Edit>();
        }

        [RelayCommand]
        private static async Task Editar(UnidadeEpidemiologicaComDetalhes ue)
        {
            await NavigationUtils.ShowViewAsModalAsync<UnidadeEpidemiologicaView_Edit>(ue);
        }

        [RelayCommand]
        private static async Task ShowLote(UnidadeEpidemiologicaComDetalhes ue)
        {
            var loteViewModel = ServiceHelper.GetRequiredService<LoteViewModel>();
            await loteViewModel.LimparFiltros();
            loteViewModel.SelectedFiltroUE = loteViewModel.UEList.FirstOrDefault(u => u.id == ue.id);
            await loteViewModel.CarregaLotes();
            WeakReferenceMessenger.Default.Send(new ShowLotesMessage());
        }
    }
}
