using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SilvaData.Models;
using SilvaData.Utilities;
using System.Linq;

namespace SilvaData.Controls
{
    /// <summary>
    /// P�gina para visualizar e gerenciar a lista de Propriedades.
    /// MIGRADO: Usa CacheService ao inv�s de DadosStatic.
    /// </summary>
    public partial class PropriedadeView : ContentPageWithLocalization
    {
        private readonly CacheService _cacheService;

        public string TextoPesquisa { get; set; }

        /// <summary>
        /// A Regional selecionada para filtrar a lista (pode ser null).
        /// </summary>
        public Regional RegionalSelecionada { get; set; }

        public ObservableCollection<Propriedade> ListaPropriedades { get; private set; }

        /// <summary>
        /// MIGRADO: Construtor agora recebe CacheService via DI
        /// </summary>
        public PropriedadeView(CacheService cacheService)
        {
            InitializeComponent();

            _cacheService = cacheService;
            ListaPropriedades = new ObservableCollection<Propriedade>();

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
            ListaPropriedades = new ObservableCollection<Propriedade>(_cacheService.PropriedadeList);

            // Notifica a UI (SfListView) que a cole��o mudou
            OnPropertyChanged(nameof(ListaPropriedades));

            // Aplica o filtro
            RefreshData();

            // Registra as mensagens para atualiza��es em tempo real
            WeakReferenceMessenger.Default.Register<PropriedadeAdicionadaMessage>(this, (recipient, message) =>
            {
                ListaPropriedades.Insert(0, message.Propriedade);
            });

            WeakReferenceMessenger.Default.Register<PropriedadeSalvaMessage>(this, (recipient, message) =>
            {
                var propriedadeAlterada = ListaPropriedades.FirstOrDefault(p => p.id == message.Propriedade.id);
                if (propriedadeAlterada != null)
                {
                    var index = ListaPropriedades.IndexOf(propriedadeAlterada);
                    if (index >= 0)
                        ListaPropriedades[index] = message.Propriedade;
                }
            });

            // ADICIONADO: Registra refresh do cache
            WeakReferenceMessenger.Default.Register<RefreshCacheMessage>(this, async (r, m) =>
            {
                if (m.Type == CacheType.Propriedades || m.Type == CacheType.All)
                {
                    ListaPropriedades = new ObservableCollection<Propriedade>(_cacheService.PropriedadeList);
                    OnPropertyChanged(nameof(ListaPropriedades));
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

            WeakReferenceMessenger.Default.Unregister<PropriedadeAdicionadaMessage>(this);
            WeakReferenceMessenger.Default.Unregister<PropriedadeSalvaMessage>(this);
            WeakReferenceMessenger.Default.Unregister<RefreshCacheMessage>(this);
        }

        #endregion

        #region Comandos

        /// <summary>
        /// Comando para navegar para a tela de edi��o de uma propriedade.
        /// </summary>
        [RelayCommand(CanExecute = nameof(PodeEditar))]
        private async Task Edit(Propriedade propriedade) => await Editar(propriedade);

        /// <summary>
        /// Comando para navegar para a tela de adi��o de uma nova propriedade.
        /// </summary>
        [RelayCommand(CanExecute = nameof(PodeAdicionar))]
        private async Task AddNew()
        {
            await NavigationUtils.ShowViewAsModalAsync<PropriedadeView_Edit>();
        }

        /// <summary>
        /// Comando para navegar para a tela de Unidades Epidemiol�gicas.
        /// </summary>
        [RelayCommand]
        private async Task ShowUnidade()
        {
            await NavigationUtils.ShowViewAsModalAsync<UnidadeEpidemiologicaView>();
        }

        /// <summary>
        /// Comando para fechar a p�gina modal atual.
        /// </summary>
        [RelayCommand]
        public async Task Voltar() => await NavigationUtils.PopModalAsync();

        /// <summary>
        /// Comando para for�ar a atualiza��o dos filtros da lista.
        /// </summary>
        [RelayCommand]
        public void AtualizaFiltros() => RefreshData();

        #endregion

        #region Permiss�es

        /// <summary>
        /// Obt�m um valor que indica se o usu�rio pode editar propriedades.
        /// </summary>
        public bool PodeEditar => Permissoes.UsuarioPermissoes?.propriedades.atualizar ?? false;

        /// <summary>
        /// Obt�m um valor que indica se o usu�rio pode adicionar propriedades.
        /// </summary>
        public bool PodeAdicionar => Permissoes.UsuarioPermissoes?.propriedades.cadastrar ?? false;

        #endregion

        #region L�gica de Filtro e Dados

        /// <summary>
        /// Atualiza o filtro da SfListView.
        /// </summary>
        public void RefreshData()
        {
            if (listaPropriedades.DataSource != null)
            {
                listaPropriedades.DataSource.Filter = filterData;
                listaPropriedades.DataSource.RefreshFilter();
            }
        }

        /// <summary>
        /// M�todo de predicado de filtro para a SfListView.
        /// </summary>
        private bool filterData(object obj)
        {
            if (obj is not Propriedade propriedade) return false;

            var displayThis = true;

            // Filtro por texto
            if (!string.IsNullOrEmpty(TextoPesquisa))
            {
                var searchBarText = LocalizationManager.RemoveDiacritics(TextoPesquisa.ToUpper());
                if (propriedade.nome == null || !LocalizationManager.RemoveDiacritics(propriedade.nome.ToUpper()).Contains(searchBarText))
                    displayThis = false;
            }

            // Filtro por Regional
            if (RegionalSelecionada != null && propriedade.regionalId != RegionalSelecionada.id)
                displayThis = false;

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
        /// Navega para a p�gina de edi��o de uma propriedade espec�fica.
        /// </summary>
        public async Task Editar(Propriedade propriedade)
        {
            await NavigationUtils.ShowViewAsModalAsync<PropriedadeView_Edit>(propriedade);
        }

        #endregion
    }
}
