using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Models;
using SilvaData.Utilities;

using System.Collections.ObjectModel;
using System.Linq;

namespace SilvaData.Controls
{
    /// <summary>
    /// P�gina para visualizar e gerenciar a lista de Propriet�rios.
    /// MIGRADO: Usa CacheService ao inv�s de DadosStatic.
    /// </summary>
    public partial class ProprietarioView : ContentPageWithLocalization
    {
        private readonly CacheService _cacheService;

        public string TextoPesquisa { get; set; }

        public ObservableCollection<Proprietario> ListaProprietarios { get; private set; }

        /// <summary>
        /// MIGRADO: Construtor agora recebe CacheService via DI
        /// </summary>
        public ProprietarioView(CacheService cacheService)
        {
            InitializeComponent();

            _cacheService = cacheService;
            ListaProprietarios = new ObservableCollection<Proprietario>();

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
            ListaProprietarios = new ObservableCollection<Proprietario>(_cacheService.ProprietarioList);

            // Notifica a UI que a cole��o mudou
            OnPropertyChanged(nameof(ListaProprietarios));

            // Aplica o filtro
            RefreshData();

            // Registra as mensagens para atualiza��es em tempo real
            WeakReferenceMessenger.Default.Register<ProprietarioAdicionadoMessage>(this, (recipient, message) =>
            {
                ListaProprietarios.Insert(0, message.Proprietario);
            });

            WeakReferenceMessenger.Default.Register<ProprietarioSalvoMessage>(this, (recipient, message) =>
            {
                var proprietarioAlterado = ListaProprietarios.FirstOrDefault(p => p.id == message.Proprietario.id);
                if (proprietarioAlterado != null)
                {
                    int index = ListaProprietarios.IndexOf(proprietarioAlterado);
                    if (index != -1)
                        ListaProprietarios[index] = message.Proprietario;
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

            WeakReferenceMessenger.Default.Unregister<ProprietarioAdicionadoMessage>(this);
            WeakReferenceMessenger.Default.Unregister<ProprietarioSalvoMessage>(this);
        }

        #endregion

        #region Comandos

        /// <summary>
        /// Comando para navegar para a tela de edi��o de um propriet�rio.
        /// </summary>
        [RelayCommand(CanExecute = nameof(PodeEditar))]
        private async Task Edit(Proprietario proprietario)
        {
            await Editar(proprietario);
        }

        /// <summary>
        /// Comando para navegar para a tela de adi��o de um novo propriet�rio.
        /// </summary>
        [RelayCommand(CanExecute = nameof(PodeAdicionar))]
        private async Task AddNew()
        {
            await NavigationUtils.ShowViewAsModalAsync<ProprietarioView_Edit>();
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
        /// Obt�m um valor que indica se o usu�rio pode editar propriet�rios.
        /// </summary>
        public bool PodeEditar => Permissoes.UsuarioPermissoes?.proprietarios.atualizar ?? false;

        /// <summary>
        /// Obt�m um valor que indica se o usu�rio pode adicionar novos propriet�rios.
        /// </summary>
        public bool PodeAdicionar => Permissoes.UsuarioPermissoes?.proprietarios.cadastrar ?? false;

        #endregion

        #region L�gica de Filtro e Dados

        /// <summary>
        /// Atualiza o filtro da SfListView.
        /// </summary>
        public void RefreshData()
        {
            if (listaProprietarios.DataSource != null)
            {
                listaProprietarios.DataSource.Filter = filterData;
                listaProprietarios.DataSource.RefreshFilter();
            }
        }

        /// <summary>
        /// M�todo de predicado de filtro para a SfListView.
        /// </summary>
        private bool filterData(object obj)
        {
            if (obj is not Proprietario proprietario) return false;

            var displayThis = true;

            if (!string.IsNullOrEmpty(TextoPesquisa))
            {
                var searchBarText = LocalizationManager.RemoveDiacritics(TextoPesquisa.ToUpper());

                if (proprietario.nome == null || !LocalizationManager.RemoveDiacritics(proprietario.nome.ToUpper()).Contains(searchBarText))
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
        /// Navega para a p�gina de edi��o de um propriet�rio espec�fico.
        /// </summary>
        public async Task Editar(Proprietario proprietario)
        {
            await NavigationUtils.ShowViewAsModalAsync<ProprietarioView_Edit>(proprietario);
        }

        #endregion
    }
}
