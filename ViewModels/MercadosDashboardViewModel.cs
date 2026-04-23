using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using SilvaData.Infrastructure;
using SilvaData.Models;
using SilvaData.Utilities;

namespace SilvaData.ViewModels
{
    public partial class MercadosDashboardViewModel : ViewModelBase
    {
        private readonly ISIWebService _webService;
        private readonly CacheService _cacheService;

        [ObservableProperty]
        private string _nomeUsuario = string.Empty;

        [ObservableProperty]
        private string _mercadoNome = string.Empty;

        public ObservableCollection<Propriedade> Clientes => _cacheService.PropriedadeList;

        public MercadosDashboardViewModel(ISIWebService webService, CacheService cacheService)
        {
            _webService = webService;
            _cacheService = cacheService;
            CarregarDados();
        }

        private void CarregarDados()
        {
            NomeUsuario = _webService.LoggedUser?.nome ?? string.Empty;

            var primeiraRegional = _cacheService.RegionalList.FirstOrDefault();
            MercadoNome = primeiraRegional?.nome ?? string.Empty;
        }

        [RelayCommand]
        private Task SelecionaCliente(Propriedade _)
        {
            // TODO: navegar para a base de dados do cliente selecionado
            return Task.CompletedTask;
        }
    }
}
