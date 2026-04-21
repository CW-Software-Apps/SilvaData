using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Models;
using ISIInstitute.Views;

using SilvaData.Utilities;

namespace SilvaData.ViewModels
{
    public partial class AtividadeViewModel : ViewModelBase
    {

        public ObservableCollection<AtividadeComDetalhes> Atividades { get; private set; }

        public ObservableCollection<AtividadeComDetalhes> AtividadesDoDia { get; private set; }



        public Command _NovaAtividadeCommand;

        public Command _ShowCalendarioCommand;

        public Command _ShowListaCommand;

        [ObservableProperty]
        public bool calendarioVisible = true; //Se true mostra calend�rio, sen�o Lista

        [ObservableProperty]

        public DateTime selectedDate;

        [ObservableProperty]
        public string subTitle;


        public Command ShowCalendarioCommand => _ShowCalendarioCommand ??= new Command(() =>
        {
            CalendarioVisible = true;
            SubTitle = "";
        });

        public Command ShowListaCommand => _ShowListaCommand ??= new Command(() =>
        {
            CalendarioVisible = false;
            SubTitle = Traducao.VejaAbaixoAListaCompletaDeAtividades;
        });

        public Command NovaAtividadeCommand => _NovaAtividadeCommand ??= new Command(async () => await NovaAtividade()
                .ConfigureAwait(false),
            () => Permissoes.UsuarioPermissoes?.atividades.cadastrar ?? false);



        public AtividadeViewModel()
        {
            AtividadesDoDia = new ObservableCollection<AtividadeComDetalhes>();

            WeakReferenceMessenger.Default.Register<AtividadeAdicionadaMessage>(this, async (recipient, message) =>
                        {
                            Atividades = new ObservableCollection<AtividadeComDetalhes>(await AtividadeComDetalhes.PegaAtividadeComDetalhesAsync());
                        });

            WeakReferenceMessenger.Default.Register<AtividadeSalvaMessage>(this, async (recipient, message) =>
            {
                Atividades = new ObservableCollection<AtividadeComDetalhes>(await AtividadeComDetalhes.PegaAtividadeComDetalhesAsync());
            });

        }

        ~AtividadeViewModel()
        {
            WeakReferenceMessenger.Default.Unregister<AtividadeAdicionadaMessage>(this);
            WeakReferenceMessenger.Default.Unregister<AtividadeSalvaMessage>(this);
        }

        private async Task NovaAtividade()
        {
            if (IsBusy) return;
            IsBusy = true;
            await NavigationUtils.ShowViewAsModalAsync<AtividadeEdit>(-1);
            IsBusy = false;
        }

        public async Task CarregaDadosAtividades()
        {
            IsBusy = true;

            List<AtividadeComDetalhes> atividades = null;

            atividades = await AtividadeComDetalhes.PegaAtividadeComDetalhesAsync();
            //await CheckNewData();

            SelectedDate = DateTime.Now.Date;

            if (atividades != null) Atividades = new ObservableCollection<AtividadeComDetalhes>(atividades);

            await CarregaAtividadesDoDia(DateTime.Now.Date);

            OnPropertyChanged(nameof(SelectedDate));
            OnPropertyChanged(nameof(TotalAtividadesPendentes));
            OnPropertyChanged(nameof(PrecisaMostrarAtividadesPendentes));

            NovaAtividadeCommand.ChangeCanExecute();

            IsBusy = false;
        }

        public async Task CarregaAtividadesDoDia(DateTime data)
        {
            IsBusy = true;

            AtividadesDoDia.Clear();

            foreach (var atividade in Atividades.Where(a => ((DateTime) a.dataHoraInicio).Date.Date == data.Date)) AtividadesDoDia.Add(atividade);

            OnPropertyChanged(nameof(AtividadesDoDia));
            OnPropertyChanged(nameof(TemAgendamentoNesseDia));
            OnPropertyChanged(nameof(NaoTemAgendamentoNesseDia));
            IsBusy = false;
        }

        public bool TemAgendamentoNesseDia => AtividadesDoDia?.Count > 0;

        public bool NaoTemAgendamentoNesseDia => !TemAgendamentoNesseDia;

        public int TotalAtividadesPendentes => Atividades?.Where(a => a.JaVenceu || a.EmAndamento)
            .Count() ?? 0;


        public bool PrecisaMostrarAtividadesPendentes => TotalAtividadesPendentes > 0;

        [RelayCommand]
        private async Task MostraDetalheAtividade(Atividade a)
        {

            if (a != null)
                await NavigationUtils.ShowViewAsModalAsync<AtividadeEdit>(a.id);

        }

    }
}
