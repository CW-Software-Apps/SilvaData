using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SilvaData.Models;
using SilvaData.Pages.PopUps;
using SilvaData.Utils;
using SilvaData.Pages;
using SilvaData.Infrastructure;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;

namespace SilvaData.ViewModels
{
    public partial class SincronizacaoPendentesViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<Alteracao> listaAlteracoes = new();

        [ObservableProperty]
        private string aguardeTexto = string.Empty;

        public static SincronizacaoPendentesViewModel? Instance { get; private set; }

        /// <summary>
        /// Número total de alterações pendentes na UI.
        /// </summary>
        public int TotalAlteracoes => ListaAlteracoes?.Sum(la => la.Qtde) ?? 0;

        public SincronizacaoPendentesViewModel()
        {
            Instance = this;

            // Torna a ObservableCollection thread-safe para updates paralelos
            BindingBase.EnableCollectionSynchronization(ListaAlteracoes, null, ObservableCollectionCallback);

            // Monitora mudanças para notificar o total (ex: badge no tab)
            ListaAlteracoes.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(TotalAlteracoes));
                WeakReferenceMessenger.Default.Send(new Utilities.SyncPendentesTotalChangedMessage(TotalAlteracoes));
            };
        }

        // Callback para sincronização thread-safe — necessário para evitar exceções em acesso concorrente
        private static void ObservableCollectionCallback(object collection, object context, Action accessMethod, bool writeAccess)
        {
            lock (collection)
            {
                accessMethod?.Invoke();
            }
        }

        /// <summary>
        /// Indica se há alterações *na interface* (pode estar desatualizado após upload)
        /// </summary>
        public bool TemAlteracoes => ListaAlteracoes.Count > 0;

        /// <summary>
        /// Data/hora da última sincronização salva nas preferências.
        /// </summary>
        public DateTime LastSync => Preferences.Get("lastsyncdatetime", DateTime.MinValue);

        /// <summary>
        /// Texto formatado exibindo quando foi a última sincronização.
        /// </summary>
        public string LastSyncronization
        {
            get
            {
                if (LastSync == DateTime.MinValue)
                    return Traducao.NuncaSincronizado;

                var diferenca = DateTime.Now - LastSync;
                var result = $"{LastSync:dd/MM/yyyy HH:mm}";

                if (diferenca.TotalSeconds < 60)
                    result += $" ({string.Format(Traducao._0SegundosAtrás, (int)diferenca.TotalSeconds)})";
                else if (diferenca.TotalMinutes < 60)
                    result += $" ({string.Format(Traducao._0MinutosAtrás, (int)diferenca.TotalMinutes)})";
                else if (diferenca.TotalHours < 24)
                    result += $" ({(int)diferenca.TotalHours}h atrás)";
                else
                    result += $" ({(int)diferenca.TotalDays}d atrás)";

                return result;
            }
        }

        /// <summary>
        /// Atualiza a lista de alterações pendentes em paralelo.
        /// Lança exceção se falhar — importante para o fluxo de upload saber que falhou.
        /// </summary>
        [RelayCommand]
        private async Task<int> AtualizaListaAlteracoes()
        {
            IsBusy = true;
            AguardeTexto = Traducao.AguardeAtualizandoDados;

            ListaAlteracoes.Clear();

            try
            {
                // Prepara todas as consultas em paralelo
                var tasks = new[]
                {
                    AdicionaSeTiverAlteracao("Proprietario", Traducao.Proprietários),
                    AdicionaSeTiverAlteracao("Regional", Traducao.Regionais),
                    AdicionaSeTiverAlteracao("Atividade", Traducao.Atividades),
                    AdicionaSeTiverAlteracao("Notificacao", Traducao.Notificações),
                    AdicionaSeTiverAlteracao("Propriedade", Traducao.Propriedades),
                    AdicionaSeTiverAlteracao("UnidadeEpidemiologica", Traducao.UnidadesEpidemiológicas),
                    AdicionaSeTiverAlteracao("Lote", Traducao.Lotes),
                    AdicionaSeTiverAlteracao("LoteForm", Traducao.FormuláriosDosLotes),
                    AdicionaSeTiverAlteracao("LoteFormImagem", Traducao.ImagensDosFormulários)
                };

                // Limpa a lista ANTES de processar, para evitar inconsistências visuais
                // (ex: manter registros antigos enquanto novos são carregados)
                ListaAlteracoes.Clear();

                // Executa todas as consultas em paralelo
                await Task.WhenAll(tasks);

                // Força notificação das propriedades dependentes
                OnPropertyChanged(nameof(TemAlteracoes));
                OnPropertyChanged(nameof(TotalAlteracoes));
                OnPropertyChanged(nameof(LastSync));
                OnPropertyChanged(nameof(LastSyncronization));

                // Atualiza badge global (ex: no tab de sincronização)
                WeakReferenceMessenger.Default.Send(new Utilities.SyncPendentesTotalChangedMessage(TotalAlteracoes));

                Debug.WriteLine($"[Sync] Atualização da lista concluída. Total pendente: {TotalAlteracoes}");

                return TotalAlteracoes;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Sync] Erro crítico ao atualizar lista de alterações: {ex}");
                // Lança a exceção para que chamadores (ex: UploadNow) saibam que falhou
                throw new InvalidOperationException("Falha ao atualizar lista de alterações pendentes", ex);
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
                AguardeTexto = string.Empty;
            }
        }

        /// <summary>
        /// Verifica se há alterações em uma tabela e adiciona à lista se houver.
        /// </summary>
        /// <param name="tabela">Nome da tabela no banco (ex: 'Proprietario')</param>
        /// <param name="texto">Texto amigável para exibição</param>
        /// <param name="filtroAdicional">Filtro SQL opcional</param>
        private async Task AdicionaSeTiverAlteracao(string tabela, string texto, string filtroAdicional = "")
        {
            try
            {
                var alteracaoInfo = await Alteracao.TotalAlteracoesTabela(tabela, filtroAdicional);
                if (alteracaoInfo?.Qtde > 0)
                {
                    alteracaoInfo.TabelaTexto = texto;
                    ListaAlteracoes.Add(alteracaoInfo);
                }
            }
            catch (Exception ex)
            {
                // Loga, mas NÃO quebra o fluxo — outras tabelas devem continuar
                Debug.WriteLine($"[Sync] Falha ao verificar alterações em {tabela}: {ex.Message}");
                // Opcional: adicionar um item de erro na UI?
            }
        }

        /// <summary>
        /// Realiza o upload de todas as alterações pendentes para o servidor.
        /// Após o upload, reconsulta o banco para garantir o estado real antes de validar sucesso.
        /// </summary>
        [RelayCommand]
        private async Task UploadNow()
        {
            if (IsBusy) return;

            IsBusy = true;
            AguardeTexto = Traducao.AguardeEnviandoDados;

            var erros = new List<string>();

            try
            {
                // Faz upload em ordem definida (evita dependências não resolvidas).
                // Erros por etapa são coletados — o processo continua nas etapas seguintes.
                await UploadDados(Traducao.Proprietários, Proprietario.UploadUpdates(), erros);
                await UploadDados(Traducao.Regionais, Regional.UploadUpdates(), erros);
                await UploadDados(Traducao.Propriedades, Propriedade.UploadUpdates(), erros);
                await UploadDados(Traducao.UnidadesEpidemiológicas, UnidadeEpidemiologica.UploadUpdates(), erros);
                await UploadDados(Traducao.Lotes, Lote.UploadUpdates(), erros);
                await UploadDados(Traducao.Atividades, Atividade.UploadUpdates(), erros);
                await UploadDados(Traducao.Notificações, Notificacao.UploadUpdates(), erros);
                await UploadDados(Traducao.FormuláriosDosLotes, LoteForm.FazUploadLoteFormsAtualizados(), erros);
                await UploadDados(Traducao.ImagensDosFormulários, LoteFormImagem.UploadUpdates(), erros);

                // ⚠️ Verifica o estado REAL no banco após todos os uploads
                var totalPendenteReal = await AtualizaListaAlteracoes();

                if (totalPendenteReal == 0 && !erros.Any())
                {
                    // ✅ SUCESSO TOTAL: Todos os dados foram enviados sem erros
                    Debug.WriteLine("[Sync] Upload concluído com sucesso — nenhum registro pendente.");

                    Preferences.Set("FormularioEmAndamento", "");

                    await Lote.ApagaLotesFechadosQueJaFizeramUploadEEstaoFechados();
                    Lote.NeedRefresh = true;

                    try
                    {
                        var cache = ServiceHelper.GetRequiredService<CacheService>();
                        await cache.PegaDadosIniciais(true);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Sync] Aviso: falha ao atualizar cache pós-upload (não crítico): {ex.Message}");
                    }

                    await PopUpOK.ShowAsync(Traducao.Sucesso, Traducao.DadosEnviadosComSucesso);
                }
                else if (totalPendenteReal == 0 && erros.Any())
                {
                    // ⚠️ SUCESSO PARCIAL: Dados enviados, mas houve erros em algumas etapas
                    Debug.WriteLine($"[Sync] Upload concluído com avisos: {erros.Count} erro(s).");

                    Preferences.Set("FormularioEmAndamento", "");
                    Lote.NeedRefresh = true;

                    var mensagem = $"{Traducao.DadosEnviadosComSucesso}\n\n⚠️ Atenção — ocorreram erros em algumas etapas:\n{string.Join("\n", erros)}";
                    await PopUpOK.ShowAsync(Traducao.Atenção, mensagem);
                }
                else
                {
                    // ❌ FALHA: Ainda há registros pendentes no banco
                    Debug.WriteLine($"[Sync] Upload concluído, mas ainda há {totalPendenteReal} registros pendentes.");

                    var detalhes = string.Join("\n", ListaAlteracoes.Select(a => $"  • {a.TabelaTexto}: {a.Qtde}"));
                    var mensagem = $"{string.Format(Traducao.AindaHa0RegistrosPendentes, totalPendenteReal)}\n\n{detalhes}";
                    if (erros.Any())
                        mensagem += $"\n\nErros:\n{string.Join("\n", erros)}";

                    await PopUpOK.ShowAsync(Traducao.Atenção, mensagem);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Sync] Erro crítico durante o upload: {ex}");
                await PopUpOK.ShowAsync(Traducao.Erro, $"{Traducao.FalhaAoEnviarDados} - {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                AguardeTexto = string.Empty;
            }
        }

        /// <summary>
        /// Executa o upload de uma etapa, coletando o erro na lista caso falhe.
        /// Não interrompe o fluxo — as demais etapas continuam sendo enviadas.
        /// </summary>
        private async Task UploadDados(string tabelaTexto, Task task, List<string> erros)
        {
            AguardeTexto = string.Format(Traducao.Enviando0, tabelaTexto);
            Debug.WriteLine($"[Sync] Iniciando upload: {tabelaTexto}");
            try
            {
                await task;
                Debug.WriteLine($"[Sync] Upload concluído: {tabelaTexto}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Sync] Erro ao enviar {tabelaTexto}: {ex.Message}");
                erros.Add($"  • {tabelaTexto}: {ex.Message}");
            }
        }

        /// <summary>
        /// Abre a tela de download (sincronização descendente) em modal.
        /// </summary>
        [RelayCommand]
        private async Task DownloadNow()
        {
            await NavigationUtils.ShowPageAsModalAsync(new SincronizacaoPageModal());
        }
    }
}