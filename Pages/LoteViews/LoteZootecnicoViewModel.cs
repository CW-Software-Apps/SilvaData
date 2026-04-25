using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utilities;
using SilvaData.Utils;

using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SilvaData.ViewModels
{
    public enum TabKind { Geral, PorIdade }

    /// <summary>
    /// ViewModel para Zootécnico.
    /// ✅ Atualiza automaticamente quando salva formulário
    /// ✅ Usa padrão de comando no item (ShowManejoCommand)
    /// ✅ Sistema de abas: Geral / Por idade
    /// </summary>
    public partial class LoteZootecnicoViewModel : ViewModelBase
    {
        [ObservableProperty] private Lote? lote;
        [ObservableProperty] private ObservableCollection<ManejoZootecnicoBox> zootecnicoList = new();
        [ObservableProperty] private ObservableCollection<LoteForm> zootecnicoGeralList = new();

        // ★ NOVO: Controle de abas
        [ObservableProperty] private int abaAtiva = 0; // 0 = Geral, 1 = Por idade
        [ObservableProperty] private bool exibeAbaGeral = true;
        [ObservableProperty] private bool exibeAbaPorIdade = false;

        // Non-localized enum selection (manual property to avoid Hot Reload issues)
        private TabKind _tabSelecionada = TabKind.Geral;
        public TabKind TabSelecionada
        {
            get => _tabSelecionada;
            set
            {
                if (_tabSelecionada != value)
                {
                    _tabSelecionada = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsAbaGeralSelecionada));
                    OnPropertyChanged(nameof(IsAbaPorIdadeSelecionada));
                }
            }
        }

        // Computed flags for XAML triggers
        public bool IsAbaGeralSelecionada => TabSelecionada == TabKind.Geral;
        public bool IsAbaPorIdadeSelecionada => TabSelecionada == TabKind.PorIdade;

        public string TextoAbaGeral => $"{Traducao.AbaGeral} ({ZootecnicoGeralList.Count})";
        public string TextoAbaPorIdade => $"{Traducao.AbaPorIdade} ({ZootecnicoList.Count})";

        public LoteZootecnicoViewModel()
        {
            WeakReferenceMessenger.Default.Register<FormularioSalvoMessage>(this, async (r, m) => await OnFormularioSalvo(m.FormularioSalvo));
        }

        /// <summary>
        /// ★ NOVO: Alterna entre abas (enum não-localizado)
        /// </summary>
        [RelayCommand]
        public void SelecionarAba(object? param)
        {
            TabKind aba;
            if (param is TabKind tk)
            {
                aba = tk;
            }
            else if (param is string s && Enum.TryParse<TabKind>(s, true, out var parsed))
            {
                aba = parsed;
            }
            else
            {
                return;
            }

            TabSelecionada = aba;
            if (aba == TabKind.Geral)
            {
                AbaAtiva = 0;
                ExibeAbaGeral = true;
                ExibeAbaPorIdade = false;
            }
            else // PorIdade
            {
                AbaAtiva = 1;
                ExibeAbaGeral = false;
                ExibeAbaPorIdade = true;
            }
        }

        private async Task OnFormularioSalvo(LoteForm? loteForm)
        {
            if (loteForm?.loteId != Lote?.id)
            {
                Debug.WriteLine($"[LoteZootecnicoViewModel] OnFormularioSalvo: loteId mismatch ({loteForm?.loteId} != {Lote?.id})");
                return;
            }
            if (loteForm?.parametroTipoId != 11)
            {
                Debug.WriteLine($"[LoteZootecnicoViewModel] OnFormularioSalvo: parametroTipoId mismatch ({loteForm?.parametroTipoId} != 11)");
                return;
            }

            Debug.WriteLine($"[LoteZootecnicoViewModel] OnFormularioSalvo: Recarregando lista geral para lote {Lote?.id}");

            try
            {
                // ★ Recarrega aba Geral
                await CarregaZootecnicoGeral();

                var caixa = ZootecnicoList.FirstOrDefault(c => c.TipoParametro == 11 && c.Fase == loteForm.loteFormFase);
                if (caixa != null)
                {
                    await caixa.PegaDados(Lote, PodeEditarManejo, PodeVerZootecnico);
                    var index = ZootecnicoList.IndexOf(caixa);
                    if (index >= 0)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            ZootecnicoList.RemoveAt(index);
                            ZootecnicoList.Insert(index, caixa);
                        });
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[LoteZootecnicoViewModel] Erro ao atualizar Zootécnico: {ex.Message}"); }
        }

        public async Task CarregaDados(Lote lote)
        {
            if (lote == null) return;

            try
            {
                IsBusy = true;
                Lote = lote;
                Lote.EnsureNames();
                ZootecnicoList.Clear();
                ZootecnicoGeralList.Clear();

                // ★ Carrega lista GERAL
                await CarregaZootecnicoGeral();

                var fases = new[] { 1, 2, 3, 4, 5, 6 };
                var dias = new[] { 7, 14, 21, 28, 35, 42 };

                for (int i = 0; i < fases.Length; i++)
                {
                    var box = new ManejoZootecnicoBox
                    {
                        Fase = fases[i],
                        Numero = dias[i],
                        TipoParametro = 11,
                        DiasDe = dias[i],
                        Nome = $"{dias[i]} {Traducao.Dias}",
                        // ★ NOVO: Adiciona comando no item (padrão do projeto)
                        ShowManejoCommand = new AsyncRelayCommand<ManejoZootecnicoBox>(ShowZootecnico)
                    };

                    await box.PegaDados(Lote, PodeEditarManejo, PodeVerZootecnico);
                    ZootecnicoList.Add(box);
                }
                OnPropertyChanged(nameof(TextoAbaPorIdade));
            }
            catch (Exception ex) { await PopUpOK.ShowAsync(Traducao.Erro, $"Erro: {ex.Message}"); }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// ★ OTIMIZADO: Carrega lista GERAL de zootécnico com pré-cache paralelo
        /// </summary>
        private async Task CarregaZootecnicoGeral()
        {
            if (Lote == null) return;

            try
            {
                // ★ Carrega formulários do banco (Fase 7 = Geral, null = Legado/Outros)
                var forms7Task = LoteForm.PegaListaFormulariosLoteList((int)Lote.id, 11, 7);
                var formsNullTask = LoteForm.PegaListaFormulariosLoteList((int)Lote.id, 11, null);

                await Task.WhenAll(forms7Task, formsNullTask);

                var forms = forms7Task.Result.Concat(formsNullTask.Result).ToList();

                if (forms == null || forms.Count == 0)
                {
                    await MainThread.InvokeOnMainThreadAsync(() => ZootecnicoGeralList.Clear());
                    return;
                }

                // ★ OTIMIZAÇÃO 1: Popula cache do Lote UMA VEZ (importante para cálculo de IdadeLote na lista)
                if (Lote.id != null)
                    await Lote.PegaLoteAsync((int)Lote.id);

                // ★ OTIMIZAÇÃO 2: Ordena por data decrescente (mais recentes primeiro)
                var formsOrdenados = forms.OrderByDescending(f => f.data).ThenByDescending(f => f.DBId).ToList();

                // ★ OTIMIZAÇÃO 3: Atualiza UI em batch único
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ZootecnicoGeralList.Clear();
                    foreach (var form in formsOrdenados)
                        ZootecnicoGeralList.Add(form);

                    OnPropertyChanged(nameof(TextoAbaGeral));
                });

                Debug.WriteLine($"[LoteZootecnicoViewModel] ✓ {formsOrdenados.Count} formulários carregados (Fase 7 e null)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoteZootecnicoViewModel] ✖ Erro ao carregar zootécnico geral: {ex.Message}");
            }
        }

        /// <summary>
        /// ★ NOVO: Abre zootécnico da lista geral
        /// </summary>
        [RelayCommand]
        public async Task ShowZootecnicoGeral(LoteForm? loteForm)
        {
            if (!PodeVerZootecnico || loteForm == null || Lote == null) return;

            await NavigationUtils.OpenLoteFormularioAsync(
                lote: Lote,
                loteFormId: loteForm.id ?? -1,
                parametroTipoId: 11,
                fase: loteForm.loteFormFase,
                isReadOnly: true,
                podeEditar: PodeEditarManejo,
                item: loteForm.item,
                modeloIsiMacroSelecionado: null,
                limpaFormularioAtual: false);
        }

        [RelayCommand]
        public async Task ShowZootecnico(ManejoZootecnicoBox? manejo)
        {
            if (!PodeVerZootecnico || manejo == null || Lote == null) return;

            await NavigationUtils.OpenLoteFormularioAsync(
                lote: Lote,
                loteFormId: manejo.LoteForm?.id ?? -1,
                parametroTipoId: 11,
                fase: manejo.Fase,
                isReadOnly: manejo.LoteForm != null,
                podeEditar: PodeEditarManejo,
                item: manejo.LoteForm?.item,
                modeloIsiMacroSelecionado: null,
                limpaFormularioAtual: manejo.LoteForm == null);
        }

        /// <summary>
        /// ★ NOVO: Abre um novo formulário de zootécnico
        /// </summary>
        [RelayCommand]
        public async Task NovoZootecnico()
        {
            if (!PodeEditarManejo || Lote == null) return;

            await NavigationUtils.OpenLoteFormularioAsync(
                lote: Lote,
                loteFormId: -1,
                parametroTipoId: 11,
                fase: 7, //Zootecnico Geral
                isReadOnly: false,
                podeEditar: true,
                item: null,
                modeloIsiMacroSelecionado: null,
                limpaFormularioAtual: true);
        }

        public bool PodeVerZootecnico => Permissoes.PodeVerManejo;
        public bool PodeEditarManejo => Permissoes.PodeEditarZootecnico;

        public override void Cleanup()
        {
            base.Cleanup();
            WeakReferenceMessenger.Default.Unregister<FormularioSalvoMessage>(this);
        }
    }
}
