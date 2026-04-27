using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Models;
using SilvaData.Utilities;

using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SilvaData.Infrastructure
{
    /// <summary>
    /// Serviço de cache centralizado que gerencia apenas listas compartilhadas.
    /// Thread-safe com suporte a sincronização de coleções.
    ///
    /// ⚠️ IMPORTANTE: Dados específicos do fluxo (Lote, UE, etc.) são gerenciados pelos ViewModels individuais.
    /// </summary>
    public partial class CacheService : ObservableObject
    {
        private bool _jaPegouDadosIniciais = false;
        private bool _estaAtualizando = false;
        private readonly object _lockObject = new object();

        // Dados em cache (compartilhados entre ViewModels)
        [ObservableProperty]
        private List<Parametro> _todosParametrosList = new();

        [ObservableProperty]
        private List<ParametroAlternativas> _todosParametrosAlternativasList = new();

        // Dicionários para busca rápida (O(1))
        private readonly Dictionary<int, UnidadeEpidemiologicaComDetalhes> _ueCache = new();
        private readonly Dictionary<int, Propriedade> _propriedadeCache = new();
        private readonly Dictionary<int, Regional> _regionalCache = new();

        // Coleções observáveis (thread-safe)
        public ObservableCollection<Propriedade> PropriedadeList { get; }
        public ObservableCollection<UnidadeEpidemiologicaComDetalhes> UEList { get; }
        public ObservableCollection<Regional> RegionalList { get; }
        public ObservableCollection<Proprietario> ProprietarioList { get; }

        public CacheService()
        {
            PropriedadeList = new ObservableCollection<Propriedade>();
            UEList = new ObservableCollection<UnidadeEpidemiologicaComDetalhes>();
            RegionalList = new ObservableCollection<Regional>();
            ProprietarioList = new ObservableCollection<Proprietario>();

            // Thread-safety com EnableCollectionSynchronization
            BindingBase.EnableCollectionSynchronization(PropriedadeList, _lockObject, CollectionSynchronizationCallback);
            BindingBase.EnableCollectionSynchronization(UEList, _lockObject, CollectionSynchronizationCallback);
            BindingBase.EnableCollectionSynchronization(RegionalList, _lockObject, CollectionSynchronizationCallback);
            BindingBase.EnableCollectionSynchronization(ProprietarioList, _lockObject, CollectionSynchronizationCallback);

            // Registra listener para mensagens de refresh
            WeakReferenceMessenger.Default.Register<RefreshCacheMessage>(this, async (r, m) =>
            {
                await HandleRefreshMessage(m);
            });
        }

        /// <summary>
        /// Callback que controla o acesso thread-safe às coleções.
        /// </summary>
        private void CollectionSynchronizationCallback(
            IEnumerable collection,
            object context,
            Action accessMethod,
            bool writeAccess)
        {
            lock (context)
            {
                accessMethod?.Invoke();
            }
        }

        private async Task HandleRefreshMessage(RefreshCacheMessage message)
        {
            switch (message.Type)
            {
                case CacheType.UnidadesEpidemiologicas:
                    await UpdateUnidadesEpidemiologicas();
                    break;
                case CacheType.Propriedades:
                    await UpdatePropriedade();
                    break;
                case CacheType.Proprietarios:
                    await UpdateProprietarios();
                    break;
                case CacheType.Regionais:
                    await UpdateRegionais();
                    break;
                case CacheType.All:
                    await PegaDadosIniciais(true);
                    break;
            }
        }

        #region Dados Iniciais

        /// <summary>
        /// Recarrega TODO o cache forçadamente.
        /// </summary>
        public async Task Refresh()
        {
            _jaPegouDadosIniciais = false;
            await PegaDadosIniciais(true).ConfigureAwait(false);
        }

        /// <summary>
        /// Carrega todos os dados iniciais do banco de dados para o cache.
        /// </summary>
        public async Task PegaDadosIniciais(bool force = false)
        {
            if ((_jaPegouDadosIniciais && !force) || _estaAtualizando)
            {
                return;
            }

            lock (_ueCache) _ueCache.Clear();
            lock (_propriedadeCache) _propriedadeCache.Clear();
            lock (_regionalCache) _regionalCache.Clear();

            _estaAtualizando = true;

            try
            {
                await Task.WhenAll(
                    UpdatePropriedade(),
                    UpdateProprietarios(),
                    UpdateRegionais(),
                    UpdateUnidadesEpidemiologicas()
                ).ConfigureAwait(false);

                _jaPegouDadosIniciais = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CacheService] Falha ao pegar dados iniciais: {ex.Message}");
                throw;
            }
            finally
            {
                _estaAtualizando = false;
            }
        }

        #endregion

        #region Atualizações de Cache

        /// <summary>
        /// Atualiza a lista de Unidades Epidemiológicas.
        /// </summary>
        public async Task UpdateUnidadesEpidemiologicas()
        {
            var items = await UnidadeEpidemiologicaComDetalhes.PegaListaUnidadesComDetalhesAsync().ConfigureAwait(false);
            SyncCollection(UEList, items);
        }

        /// <summary>
        /// Atualiza a lista de Regionais.
        /// </summary>
        public async Task UpdateRegionais()
        {
            var items = await Regional.PegaListaRegionaisAsync().ConfigureAwait(false);
            SyncCollection(RegionalList, items);
        }

        /// <summary>
        /// Atualiza a lista de Propriedades.
        /// </summary>
        public async Task UpdatePropriedade()
        {
            var items = await Propriedade.PegaListaPropriedadesAsync().ConfigureAwait(false);
            SyncCollection(PropriedadeList, items);
        }

        /// <summary>
        /// Atualiza a lista de Proprietários.
        /// </summary>
        public async Task UpdateProprietarios()
        {
            var items = await Proprietario.PegaListaProprietarioAsync().ConfigureAwait(false);
            SyncCollection(ProprietarioList, items);
        }

        /// <summary>
        /// Sincroniza coleções de forma thread-safe no MainThread e atualiza dicionários de busca rápida.
        /// Optimized: Uses HashSet for O(1) lookups instead of O(n) Contains checks.
        /// </summary>
        private void SyncCollection<T>(ObservableCollection<T> collection, List<T> newItems)
        {
            // Atualiza dicionários de busca rápida (em background thread mesmo, antes do MainThread)
            UpdateInternalDictionaries(newItems);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                lock (_lockObject)
                {
                    // Optimized: Use HashSet for O(1) lookups instead of multiple enumeration
                    var newItemsSet = new HashSet<T>(newItems);
                    
                    // Remove items that are no longer in the new set
                    var itemsToRemove = new List<T>();
                    foreach (var item in collection)
                    {
                        if (!newItemsSet.Contains(item))
                        {
                            itemsToRemove.Add(item);
                        }
                    }
                    
                    foreach (var item in itemsToRemove)
                    {
                        collection.Remove(item);
                    }

                    // Add new items that aren't in the collection
                    var existingSet = new HashSet<T>(collection);
                    foreach (var item in newItems)
                    {
                        if (!existingSet.Contains(item))
                        {
                            collection.Add(item);
                        }
                    }
                }
            });
        }

        #endregion

        #region Limpeza

        /// <summary>
        /// Limpa TODOS os dados do cache.
        /// Deve ser chamado quando o usuário faz logout.
        /// </summary>
        public void ClearAllData()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                lock (_lockObject)
                {
                    ProprietarioList.Clear();
                    RegionalList.Clear();
                    PropriedadeList.Clear();
                    UEList.Clear();

                    lock (_ueCache) _ueCache.Clear();
                    lock (_propriedadeCache) _propriedadeCache.Clear();
                    lock (_regionalCache) _regionalCache.Clear();

                    TodosParametrosList.Clear();
                    TodosParametrosAlternativasList.Clear();

                    _jaPegouDadosIniciais = false;
                    _estaAtualizando = false;
                }
            });
        }

        private void UpdateInternalDictionaries<T>(List<T> items)
        {
            if (items == null) return;

            if (typeof(T) == typeof(UnidadeEpidemiologicaComDetalhes))
            {
                lock (_ueCache)
                {
                    foreach (var item in items.Cast<UnidadeEpidemiologicaComDetalhes>())
                        _ueCache[item.id] = item;
                }
            }
            else if (typeof(T) == typeof(Propriedade))
            {
                lock (_propriedadeCache)
                {
                    foreach (var item in items.Cast<Propriedade>())
                        if (item.id.HasValue) _propriedadeCache[item.id.Value] = item;
                }
            }
            else if (typeof(T) == typeof(Regional))
            {
                lock (_regionalCache)
                {
                    foreach (var item in items.Cast<Regional>())
                        if (item.id.HasValue) _regionalCache[item.id.Value] = item;
                }
            }
        }

        public UnidadeEpidemiologicaComDetalhes? GetUE(int id)
        {
            lock (_ueCache) return _ueCache.TryGetValue(id, out var ue) ? ue : null;
        }

        public Propriedade? GetPropriedade(int id)
        {
            lock (_propriedadeCache) return _propriedadeCache.TryGetValue(id, out var p) ? p : null;
        }

        public Regional? GetRegional(int id)
        {
            lock (_regionalCache) return _regionalCache.TryGetValue(id, out var r) ? r : null;
        }

        #endregion
    }
}
