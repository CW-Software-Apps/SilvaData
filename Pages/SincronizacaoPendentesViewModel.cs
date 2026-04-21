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
        /// N�mero total de altera��es pendentes na UI.
        /// </summary>
        public int TotalAlteracoes => ListaAlteracoes?.Sum(la => la.Qtde) ?? 0;

        public SincronizacaoPendentesViewModel()
        {
            Instance = this;

            // Torna a ObservableCollection thread-safe para updates paralelos
            BindingBase.EnableCollectionSynchronization(ListaAlteracoes, null, ObservableCollectionCallback);

            // Monitora mudan�as para notificar o total (ex: badge no tab)
            ListaAlteracoes.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(TotalAlteracoes));
                WeakReferenceMessenger.Default.Send(new Utilities.SyncPendentesTotalChangedMessage(TotalAlteracoes));
            };
        }

        // Callback para sincroniza��o thread-safe � necess�rio para evitar exce��es em acesso concorrente
        private static void ObservableCollectionCallback(object collection, object context, Action accessMethod, bool writeAccess)
        {
            lock (collection)
            {
                accessMethod?.Invoke();
            }
        }

        /// <summary>
        /// Indica se h� altera��es *na interface* (pode estar desatualizado ap�s upload)
        /// </summary>
        public bool TemAlteracoes => ListaAlteracoes.Count > 0;

        /// <summary>
        /// Data/hora da �ltima sincroniza��o salva nas prefer�ncias.
        /// </summary>
        public DateTime LastSync => Preferences.Get("lastsyncdatetime", DateTime.MinValue);

        /// <summary>
        /// Texto formatado exibindo quando foi a �ltima sincroniza��o.
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
                    result += $" ({string.Format(Traducao._0SegundosAtr�s, (int)diferenca.TotalSeconds)})";
                else if (diferenca.TotalMinutes < 60)
                    result += $" ({string.Format(Traducao._0MinutosAtr�s, (int)diferenca.TotalMinutes)})";
                else if (diferenca.TotalHours < 24)
                    result += $" ({(int)diferenca.TotalHours}h atr�s)";
                else
                    result += $" ({(int)diferenca.TotalDays}d atr�s)";

                return result;
            }
        }

        /// <summary>
        /// Atualiza a lista de altera��es pendentes em paralelo.
        /// Lan�a exce��o se falhar � importante para o fluxo de upload saber que falhou.
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
                    AdicionaSeTiverAlteracao("Proprietario", Traducao.Propriet�rios),
                    AdicionaSeTiverAlteracao("Regional", Traducao.Regionais),
                    AdicionaSeTiverAlteracao("Atividade", Traducao.Atividades),
                    AdicionaSeTiverAlteracao("Notificacao", Traducao.Notifica��es),
                    AdicionaSeTiverAlteracao("Propriedade", Traducao.Propriedades),
                    AdicionaSeTiverAlteracao("UnidadeEpidemiologica", Traducao.UnidadesEpidemiol�gicas),
                    AdicionaSeTiverAlteracao("Lote", Traducao.Lotes),
                    AdicionaSeTiverAlteracao("LoteForm", Traducao.Formul�riosDosLotes),
                    AdicionaSeTiverAlteracao("LoteFormImagem", Traducao.ImagensDosFormul�rios)
                };

                // Limpa a lista ANTES de processar, para evitar inconsist�ncias visuais
                // (ex: manter registros antigos enquanto novos s�o carregados)
                ListaAlteracoes.Clear();

                // Executa todas as consultas em paralelo
                await Task.WhenAll(tasks);

                // For�a notifica��o das propriedades dependentes
                OnPropertyChanged(nameof(TemAlteracoes));
                OnPropertyChanged(nameof(TotalAlteracoes));
                OnPropertyChanged(nameof(LastSync));
                OnPropertyChanged(nameof(LastSyncronization));

                // Atualiza badge global (ex: no tab de sincroniza��o)
                WeakReferenceMessenger.Default.Send(new Utilities.SyncPendentesTotalChangedMessage(TotalAlteracoes));

                Debug.WriteLine($"[Sync] Atualiza��o da lista conclu�da. Total pendente: {TotalAlteracoes}");

                return TotalAlteracoes;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Sync] Erro cr�tico ao atualizar lista de altera��es: {ex}");
                // Lan�a a exce��o para que chamadores (ex: UploadNow) saibam que falhou
                throw new InvalidOperationException("Falha ao atualizar lista de altera��es pendentes", ex);
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
                AguardeTexto = string.Empty;
            }
        }

        /// <summary>
        /// Verifica se h� altera��es em uma tabela e adiciona � lista se houver.
        /// </summary>
        /// <param name="tabela">Nome da tabela no banco (ex: 'Proprietario')</param>
        /// <param name="texto">Texto amig�vel para exibi��o</param>
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
                // Loga, mas N�O quebra o fluxo � outras tabelas devem continuar
                Debug.WriteLine($"[Sync] Falha ao verificar altera��es em {tabela}: {ex.Message}");
                // Opcional: adicionar um item de erro na UI?
            }
        }

        /// <summary>
        /// Realiza o upload de todas as altera��es pendentes para o servidor.
        /// Ap�s o upload, reconsulta o banco para garantir o estado real antes de validar sucesso.
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
                // Faz upload em ordem definida (evita depend�ncias n�o resolvidas).
                // Erros por etapa s�o coletados � o processo continua nas etapas seguintes.
                await UploadDados(Traducao.Propriet�rios, Proprietario.UploadUpdates(), erros);
                await UploadDados(Traducao.Regionais, Regional.UploadUpdates(), erros);
                await UploadDados(Traducao.Propriedades, Propriedade.UploadUpdates(), erros);
                await UploadDados(Traducao.UnidadesEpidemiol�gicas, UnidadeEpidemiologica.UploadUpdates(), erros);
                await UploadDados(Traducao.Lotes, Lote.UploadUpdates(), erros);
                await UploadDados(Traducao.Atividades, Atividade.UploadUpdates(), erros);
                await UploadDados(Traducao.Notifica��es, Notificacao.UploadUpdates(), erros);
                await UploadDados(Traducao.Formul�riosDosLotes, LoteForm.FazUploadLoteFormsAtualizados(), erros);
                await UploadDados(Traducao.ImagensDosFormul�rios, LoteFormImagem.UploadUpdates(), erros);

                // ?? Verifica o estado REAL no banco ap�s todos os uploads
                var totalPendenteReal = await AtualizaListaAlteracoes();

                if (totalPendenteReal == 0)
                {
                    // ? SUCESSO: Todos os dados foram enviados
                    Debug.WriteLine("[Sync] Upload conclu�do com sucesso � nenhum registro pendente.");

                    // Limpa estado de formul�rio em andamento
                    Preferences.Set("FormularioEmAndamento", "");

                    // Remove lotes fechados que j� subiram (otimiza��o de espa�o)
                    await Lote.ApagaLotesFechadosQueJaFizeramUploadEEstaoFechados();
                    Lote.NeedRefresh = true;

                    // Atualiza cache local com dados mais recentes do servidor
                    try
                    {
                        var cache = ServiceHelper.GetRequiredService<ICacheService>();
                        await cache.PegaDadosIniciais(forceRefresh: true);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Sync] Aviso: falha ao atualizar cache p�s-upload (n�o cr�tico): {ex.Message}");
                    }

                    await PopUpOK.ShowAsync(Traducao.Sucesso, Traducao.DadosEnviadosComSucesso);
                }
                else
                {
                    // ? FALHA: Ainda h� registros pendentes no banco
                    Debug.WriteLine($"[Sync] Upload conclu�do, mas ainda h� {totalPendenteReal} registros pendentes.");

                    var detalhes = string.Join("\n", ListaAlteracoes.Select(a => $"  � {a.TabelaTexto}: {a.Qtde}"));
                    var mensagem = $"{string.Format(Traducao.AindaHa0RegistrosPendentes, totalPendenteReal)}\n\n{detalhes}";
                    if (erros.Any())
                        mensagem += $"\n\nErros:\n{string.Join("\n", erros)}";

                    await PopUpOK.ShowAsync(Traducao.Aten��o, mensagem);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Sync] Erro cr�tico durante o upload: {ex}");
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
        /// N�o interrompe o fluxo � as demais etapas continuam sendo enviadas.
        /// </summary>
        private async Task UploadDados(string tabelaTexto, Task task, List<string> erros)
        {
            AguardeTexto = string.Format(Traducao.Enviando0, tabelaTexto);
            Debug.WriteLine($"[Sync] Iniciando upload: {tabelaTexto}");
            try
            {
                await task;
                Debug.WriteLine($"[Sync] Upload conclu�do: {tabelaTexto}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Sync] Erro ao enviar {tabelaTexto}: {ex.Message}");
                erros.Add($"  � {tabelaTexto}: {ex.Message}");
            }
        }

        /// <summary>
        /// Abre a tela de download (sincroniza��o descendente) em modal.
        /// </summary>
        [RelayCommand]
        private async Task DownloadNow()
        {
            await NavigationUtils.ShowPageAsModalAsync(new SincronizacaoPageModal());
        }
    }
}
