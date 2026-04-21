using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Models;
using SilvaData.Utilities;

using System.Collections.ObjectModel;
using System.Linq;

namespace SilvaData.Controls
{
    /// <summary>
    /// P�gina para visualizar e gerenciar a lista de Regionais.
    /// MIGRADO: Usa CacheService ao inv�s de DadosStatic.
    /// </summary>
    public partial class RegionalView : ContentPageWithLocalization
    {
        private readonly CacheService _cacheService;

        public string TextoPesquisa { get; set; }

        public ObservableCollection<Regional> ListaRegionais { get; private set; }

        /// <summary>
        /// MIGRADO: Construtor agora recebe CacheService via DI
        /// </summary>
        public RegionalView(CacheService cacheService)
        {
            InitializeComponent();

            _cacheService = cacheService;
            ListaRegionais = new ObservableCollection<Regional>();

            BindingContext = this;
        }

        #region Ciclo de Vida da P�gina

        /// <summary>
        /// Chamado quando a p�gina est� prestes a se tornar vis�vel.
        /// Carrega os dados e registra os receptores de mensagens.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();

            // MIGRADO: Usa CacheService ao inv�s de DadosStatic
            ListaRegionais = new ObservableCollection<Regional>(_cacheService.RegionalList);

            // Notifica a UI (o SfListView) que a propriedade da cole��o mudou.
            OnPropertyChanged(nameof(ListaRegionais));

            // Aplica o filtro
            RefreshData();

            // Registra as mensagens para atualiza��es em tempo real
            WeakReferenceMessenger.Default.Register<RegionalAdicionadaMessage>(this, (recipient, message) =>
            {
                ListaRegionais.Insert(0, message.Regional);
            });

            WeakReferenceMessenger.Default.Register<RegionalSalvaMessage>(this, (recipient, message) =>
            {
                var regionalAlterada = ListaRegionais.FirstOrDefault(p => p.id == message.Regional.id);
                if (regionalAlterada != null)
                {
                    int index = ListaRegionais.IndexOf(regionalAlterada);
                    if (index != -1)
                        ListaRegionais[index] = message.Regional;
                }
            });

            // ADICIONADO: Registra refresh do cache
            WeakReferenceMessenger.Default.Register<RefreshCacheMessage>(this, async (r, m) =>
            {
                if (m.Type == CacheType.Regionais || m.Type == CacheType.All)
                {
                    ListaRegionais = new ObservableCollection<Regional>(_cacheService.RegionalList);
                    OnPropertyChanged(nameof(ListaRegionais));
                    RefreshData();
                }
            });
        }

        /// <summary>
        /// Chamado quando a p�gina n�o est� mais vis�vel.
        /// Remove o registro dos receptores de mensagens.
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            WeakReferenceMessenger.Default.Unregister<RegionalAdicionadaMessage>(this);
            WeakReferenceMessenger.Default.Unregister<RegionalSalvaMessage>(this);
            WeakReferenceMessenger.Default.Unregister<RefreshCacheMessage>(this);
        }

        #endregion

        #region Comandos

        /// <summary>
        /// Comando para navegar para a tela de edi��o de uma regional.
        /// </summary>
        [RelayCommand(CanExecute = nameof(PodeEditar))]
        private async Task Edit(Regional regional)
        {
            await Editar(regional);
        }

        /// <summary>
        /// Comando para navegar para a tela de adi��o de uma nova regional.
        /// </summary>
        [RelayCommand(CanExecute = nameof(PodeAdicionar))]
        private async Task AddNew()
        {
            await NavigationUtils.ShowViewAsModalAsync<RegionalView_Edit>();
        }

        /// <summary>
        /// Comando para fechar a p�gina modal atual.
        /// </summary>
        [RelayCommand]
        public async Task Voltar()
        {
            await NavigationUtils.PopModalAsync();
        }

        #endregion

        #region Permiss�es

        /// <summary>
        /// Obt�m um valor que indica se o usu�rio pode editar regionais.
        /// </summary>
        public bool PodeEditar => Permissoes.UsuarioPermissoes?.regionais.atualizar ?? false;

        /// <summary>
        /// Obt�m um valor que indica se o usu�rio pode adicionar novas regionais.
        /// </summary>
        public bool PodeAdicionar => Permissoes.UsuarioPermissoes?.regionais.cadastrar ?? false;

        #endregion

        #region L�gica de Filtro e Dados

        /// <summary>
        /// Atualiza o filtro da SfListView.
        /// </summary>
        public void RefreshData()
        {
            // Assumindo que 'listaRegionais' � o x:Name do SfListView no XAML
            if (listaRegionais.DataSource != null)
            {
                listaRegionais.DataSource.Filter = filterData;
                listaRegionais.DataSource.RefreshFilter();
            }
        }

        /// <summary>
        /// M�todo de predicado de filtro para a SfListView.
        /// </summary>
        private bool filterData(object obj)
        {
            if (obj is not Regional regional) return false;

            var displayThis = true;

            if (!string.IsNullOrEmpty(TextoPesquisa))
            {
                var searchBarText = LocalizationManager.RemoveDiacritics(TextoPesquisa.ToUpper());

                if (regional.nome == null || !LocalizationManager.RemoveDiacritics(regional.nome.ToUpper()).Contains(searchBarText))
                    displayThis = false;
            }
            return displayThis;
        }

        /// <summary>
        /// Manipulador de evento para altera��o de texto na barra de pesquisa.
        /// </summary>
        private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextoPesquisa = e.NewTextValue;
            RefreshData();
        }

        /// <summary>
        /// Navega para a p�gina de edi��o de uma regional espec�fica.
        /// </summary>
        public async Task Editar(Regional regional)
        {
            await NavigationUtils.ShowViewAsModalAsync<RegionalView_Edit>(regional);
        }

        #endregion
    }
}
