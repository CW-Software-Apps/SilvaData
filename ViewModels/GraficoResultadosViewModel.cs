using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using SilvaData.Models;
using SilvaData.Utilities;

using Syncfusion.Maui.Toolkit.Charts;

using System.Collections.ObjectModel;

namespace SilvaData.ViewModels
{
    /// <summary>
    /// ViewModel para a aba "Meus Resultados" (Gr�ficos).
    /// </summary>
    public partial class GraficoResultadosViewModel : ViewModelBase
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Title))]
        [NotifyPropertyChangedFor(nameof(ISIScoreTotalVisible))]
        [NotifyPropertyChangedFor(nameof(ISIDispersaoScoreVisible))]
        [NotifyPropertyChangedFor(nameof(ISIAcometentoVisible))]
        [NotifyPropertyChangedFor(nameof(GraficoCategoriaVisible))]
        [NotifyPropertyChangedFor(nameof(GraficoParametroVisible))]
        [NotifyPropertyChangedFor(nameof(GraficoSuperCategoriaVisible))]
        [NotifyPropertyChangedFor(nameof(PodeVoltarParaSuperCategoria))]
        [NotifyPropertyChangedFor(nameof(PodeVoltarParaCategoria))]
        private DashboardTipoGrafico tipoGrafico = DashboardTipoGrafico.ISIScoreTotal;

        // N�vel atual dentro do conjunto ISI Score Total (drilldown)
        private GraficoNivel nivelAtual = GraficoNivel.SuperCategoria;

        // Propriedades de Visibilidade
        public bool ISIScoreTotalVisible => TipoGrafico == DashboardTipoGrafico.ISIScoreTotal;
        public bool ISIDispersaoScoreVisible => TipoGrafico == DashboardTipoGrafico.ISIDispersaoScore;
        public bool ISIAcometentoVisible => TipoGrafico == DashboardTipoGrafico.Acometimento;
        public bool GraficoCategoriaVisible => TipoGrafico == DashboardTipoGrafico.ISIScoreTotal && nivelAtual == GraficoNivel.Categoria;
        public bool GraficoParametroVisible => TipoGrafico == DashboardTipoGrafico.ISIScoreTotal && nivelAtual == GraficoNivel.Parametro;
        public bool GraficoSuperCategoriaVisible => TipoGrafico == DashboardTipoGrafico.ISIScoreTotal && nivelAtual == GraficoNivel.SuperCategoria;
        public bool PodeVoltarParaSuperCategoria => TipoGrafico == DashboardTipoGrafico.ISIScoreTotal && (nivelAtual == GraficoNivel.Categoria || nivelAtual == GraficoNivel.Parametro);
        public bool PodeVoltarParaCategoria => TipoGrafico == DashboardTipoGrafico.ISIScoreTotal && nivelAtual == GraficoNivel.Parametro;
        public string Title => GetTitle();

        [ObservableProperty] double acometimentoMaximo;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(PrecisaMostraFiltros))] private bool isLandscape;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(PrecisaMostraFiltros))] private bool filtroVisible = false;
        public bool PrecisaMostraFiltros => !IsLandscape && FiltroVisible;

        // Cole��es de S�ries dos Gr�ficos
        [ObservableProperty] ChartSeriesCollection acometimentoSeriesCollection = new();
        [ObservableProperty] ChartSeriesCollection superCategoriaSeriesCollection = new();
        [ObservableProperty] ChartSeriesCollection categoriaSeriesCollection = new();
        [ObservableProperty] ChartSeriesCollection parametroSeriesCollection = new();

        [ObservableProperty]
        private Graficos _graficos = new();

        // Filtros
        [ObservableProperty]
        private Regional? filtroRegionalSelecionada;
        [ObservableProperty]
        private UnidadeEpidemiologica? filtroUESelecionada;

        public string SuperCategoriaSelecionada { get; set; } = string.Empty;
        public string CategoriaSelecionada { get; set; } = string.Empty;

        public string LastSyncText
        {
            get
            {
                var lastSync = Preferences.Get("lastsyncdashboard", DateTime.MinValue);
                if (lastSync == DateTime.MinValue) return Traducao.NuncaSincronizado;
                var diferenca = DateTime.Now - lastSync;
                var result = $"{lastSync.ToShortDateString()} {lastSync.ToShortTimeString()}";

                if (diferenca.TotalSeconds < 60)
                    result += $" ({string.Format(Traducao._0SegundosAtr�s, (int)diferenca.TotalSeconds)})";
                else
                    result += $" ({string.Format(Traducao._0MinutosAtr�s, (int)diferenca.TotalMinutes)})";

                return result;
            }
        }

        public GraficoResultadosViewModel()
        {
            var mainDisplayInfo = DeviceDisplay.Current.MainDisplayInfo;
            IsLandscape = mainDisplayInfo.Orientation == DisplayOrientation.Landscape;

            WeakReferenceMessenger.Default.Register<AtualizarGraficosMessage>(this, (r, m) =>
            {
                AtualizaGraficos(m.Graficos);
            });
            WeakReferenceMessenger.Default.Register<ShowGraficoMessage>(this, (r, m) =>
            {
                SetTipoGrafico(m.TipoGrafico);
            });

            // Carrega dados iniciais na primeira abertura
            _ = AtualizaAgora();
        }

        private void SetTipoGrafico(DashboardTipoGrafico tipo)
        {
            TipoGrafico = tipo;
            if (tipo == DashboardTipoGrafico.ISIScoreTotal || tipo == DashboardTipoGrafico.Acometimento || tipo == DashboardTipoGrafico.ISIDispersaoScore)
            {
                // Reinicia drilldown ao entrar em ISI Score Total
                if (tipo == DashboardTipoGrafico.ISIScoreTotal)
                    nivelAtual = GraficoNivel.SuperCategoria;

                AtualizaGraficos();
            }
        }

        public void AtualizarOrientacao(double width, double height)
        {
            IsLandscape = width > height;
        }

        [RelayCommand]
        public void ChangeLandScapeMode()
        {
            IsLandscape = !IsLandscape;
            if (IsLandscape)
                WeakReferenceMessenger.Default.Send(new SetLandscapeModeOnMessage());
            else
                WeakReferenceMessenger.Default.Send(new SetLandscapeModeOffMessage());
        }

        /// <summary>
        /// (Este comando � chamado pelo Pai: DashboardViewModel)
        /// Tamb�m faz download dos dados na web antes de redesenhar.
        /// </summary>
        [RelayCommand]
        public async Task AtualizaAgora()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var resultado = await Graficos.DownloadDadosGraficos();
                // Mesmo se falhar, tenta redesenhar com cache local (se existir)
                AtualizaGraficos();
                Preferences.Set("lastsyncdashboard", DateTime.Now);
                OnPropertyChanged(nameof(LastSyncText));
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Chamado pelos filtros ou pela atualiza��o para redesenhar os gr�ficos.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        public void AtualizaGraficos()
        {
            var regionalNome = FiltroRegionalSelecionada?.nome ?? "";
            if (regionalNome.Equals("Todos") || regionalNome.Equals("Todas")) regionalNome = "";

            var ueNome = FiltroUESelecionada?.nome ?? "";
            if (ueNome.Equals("Todos") || ueNome.Equals("Todas")) ueNome = "";

            Graficos.GeraGraficoSuperCategoria(regionalNome, ueNome);
            Graficos.GeraGraficoAcometimento(regionalNome, ueNome);
            Graficos.GeraGraficoDispersao(regionalNome, ueNome);
            GeraCoresGraficosDispersao();

            // Super Categoria
            var tempSuperCategoria = new ChartSeriesCollection();
            if (Graficos.ListaSuperCategorias != null)
            {
                foreach (var item in Graficos.ListaSuperCategorias.Where(l => l.Count > 0))
                {
                    var grafico = new StackingColumnSeries
                    {
                        ItemsSource = item,
                        Label = item[0].SuperCategoria,
                        XBindingPath = "dataLote",
                        YBindingPath = "MediaScore",
                        ShowDataLabels = true,
                        EnableTooltip = true,
                        EnableAnimation = true,
                        DataLabelSettings = GetDataLabelSettings(),
                        TooltipTemplate = GetResourceTemplate("SuperCategoriaTemplate")
                    };
                    grafico.Fill = CoresGrafico.Instance.PegaCor(item[0].SuperCategoriaId, item[0].SuperCategoria, 1);

                    tempSuperCategoria.Add(grafico);
                }
            }
            SuperCategoriaSeriesCollection = tempSuperCategoria;

            // Acometimento (LineSeries)
            var tempAcometimento = new ChartSeriesCollection();
            double max = -1;
            if (Graficos.ListaAcometimentos != null)
            {
                foreach (var item in Graficos.ListaAcometimentos.Where(l => l.Count > 0))
                {
                    foreach (var subitem in item)
                        if (subitem.PorcentagemAcometimento > max) max = subitem.PorcentagemAcometimento;

                    var grafico = new LineSeries()
                    {
                        ItemsSource = item,
                        Label = item[0].SuperCategoria,
                        XBindingPath = "dataLote",
                        YBindingPath = "PorcentagemAcometimento",
                        EnableTooltip = false,
                        EnableAnimation = true,
                    };
                    grafico.Fill = CoresGrafico.Instance.PegaCor(item[0].SuperCategoriaId, item[0].SuperCategoria, 1);
                    tempAcometimento.Add(grafico);
                }
            }
            AcometimentoSeriesCollection = tempAcometimento;
            AcometimentoMaximo = (max == -1) ? 100 : max;

            OnPropertyChanged(nameof(Graficos));
        }

        /// <summary>
        /// (Este m�todo � chamado pelo Messenger)
        /// </summary>
        public void AtualizaGraficos(Graficos graficos)
        {
            Graficos = graficos;
            AtualizaGraficos();
            OnPropertyChanged(nameof(LastSyncText));
        }

        private void GeraCoresGraficosDispersao()
        {
            Graficos.DadosGraficosDispersaoCores = new ObservableCollection<Brush>();
            if (Graficos.DadosGraficosDispersao == null) return;

            foreach (var item in Graficos.DadosGraficosDispersao)
            {
                Color color = item.Score switch
                {
                    <= 20 => Colors.Green,
                    < 31 => Colors.Orange,
                    _ => Colors.Red
                };
                Graficos.DadosGraficosDispersaoCores.Add(new SolidColorBrush(color));
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        internal void MostraGraficoCategoria(string superCategoria)
        {
            SuperCategoriaSelecionada = superCategoria;
            Graficos.AtualizaGraficoCategoria(superCategoria);

            var tempCategoria = new ChartSeriesCollection();
            foreach (var item in Graficos.ListaCategorias.Where(l => l.Count > 0))
            {
                var grafico = new StackingColumnSeries
                {
                    ItemsSource = item,
                    Label = item[0].Categoria,
                    XBindingPath = "dataLote",
                    YBindingPath = "MediaScore",
                    ShowDataLabels = true,
                    EnableTooltip = true,
                    EnableAnimation = true,
                    DataLabelSettings = GetDataLabelSettings(),
                    TooltipTemplate = GetResourceTemplate("CategoriaTemplate")
                };
                grafico.Fill = CoresGrafico.Instance.PegaCor(item[0].CategoriaId, item[0].Categoria, 2);

                tempCategoria.Add(grafico);
            }
            CategoriaSeriesCollection = tempCategoria;
            nivelAtual = GraficoNivel.Categoria;
            OnPropertyChanged(nameof(GraficoCategoriaVisible));
            OnPropertyChanged(nameof(GraficoSuperCategoriaVisible));
            OnPropertyChanged(nameof(PodeVoltarParaSuperCategoria));
        }

        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        internal void MostraGraficoParametro(string categoria)
        {
            CategoriaSelecionada = categoria;
            Graficos.AtualizaGraficoParametro(categoria);

            var tempParametro = new ChartSeriesCollection();
            foreach (var item in Graficos.ListaParametros.Where(l => l.Count > 0))
            {
                var grafico = new StackingColumnSeries
                {
                    ItemsSource = item,
                    Label = item[0].Parametro,
                    XBindingPath = "dataLote",
                    YBindingPath = "MediaScore",
                    ShowDataLabels = true,
                    EnableTooltip = true,
                    EnableAnimation = true,
                    DataLabelSettings = GetDataLabelSettings(),
                    TooltipTemplate = GetResourceTemplate("PropriedadeTemplate")
                };
                grafico.Fill = CoresGrafico.Instance.PegaCor(item[0].ParametroId, item[0].Parametro, 3);

                tempParametro.Add(grafico);
            }
            ParametroSeriesCollection = tempParametro;
            nivelAtual = GraficoNivel.Parametro;
            OnPropertyChanged(nameof(GraficoParametroVisible));
            OnPropertyChanged(nameof(GraficoCategoriaVisible));
            OnPropertyChanged(nameof(PodeVoltarParaCategoria));
        }

        #region Comandos de Voltar (Drill-up)

        [RelayCommand]
        private void VoltaParaSuperCategoria()
        {
            nivelAtual = GraficoNivel.SuperCategoria;
            OnPropertyChanged(nameof(GraficoSuperCategoriaVisible));
            OnPropertyChanged(nameof(GraficoCategoriaVisible));
            OnPropertyChanged(nameof(GraficoParametroVisible));
            OnPropertyChanged(nameof(PodeVoltarParaSuperCategoria));
            OnPropertyChanged(nameof(PodeVoltarParaCategoria));
        }

        [RelayCommand]
        private void VoltaParaCategoria()
        {
            nivelAtual = GraficoNivel.Categoria;
            OnPropertyChanged(nameof(GraficoSuperCategoriaVisible));
            OnPropertyChanged(nameof(GraficoCategoriaVisible));
            OnPropertyChanged(nameof(GraficoParametroVisible));
            OnPropertyChanged(nameof(PodeVoltarParaSuperCategoria));
            OnPropertyChanged(nameof(PodeVoltarParaCategoria));
        }

        #endregion

        [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
        public async Task ShowHideFiltro()
        {
            FiltroVisible = !FiltroVisible;
        }

        public override async Task Voltar()
        {
            WeakReferenceMessenger.Default.Send(new ShowLotesMessage());
        }

        private bool CanExecuteCommands() => !IsBusy;

        private string GetTitle() => TipoGrafico switch
        {
            DashboardTipoGrafico.ISIScoreTotal => Traducao.ISIScoreTotal,
            DashboardTipoGrafico.Acometimento => Traducao.Acometimento,
            DashboardTipoGrafico.ISIDispersaoScore => "Dispers�o",
            _ => "Resultados"
        };

        #region Helpers de Recursos

        private static CartesianDataLabelSettings GetDataLabelSettings()
        {
            // ChartDataLabelSettings is abstract in the Toolkit. Use CartesianDataLabelSettings for cartesian series.
            return new CartesianDataLabelSettings
            {
                LabelStyle = new ChartDataLabelStyle
                {
                    TextColor = GetResourceColor("PrimaryColor", Colors.White),
                    Background = GetResourceColor("FundoClarinhoBotao", Colors.Transparent)
                }
            };
        }

        private static Color GetResourceColor(string key, Color fallback)
        {
            if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color c)
                return c;
            return fallback;
        }

        private static DataTemplate? GetResourceTemplate(string key)
        {
            if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is DataTemplate dt)
                return dt;
            return null;
        }

        #endregion
    }
}
