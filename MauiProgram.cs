using CommunityToolkit.Maui;

using ISIInstitute.Views;
using ISIInstitute.Views.LoteViews;

using SilvaData.Pages.PopUps;
using SilvaData.Services;
using SilvaData.ViewModels;
using SilvaData.Utilities;
using SilvaData.Controls;
using SilvaData.Pages.LoteViews;

using LocalizationResourceManager.Maui;

using Microsoft.Extensions.Logging;

using Syncfusion.Maui.Core.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;

namespace SilvaData
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSentry(options =>
                {
                    options.Dsn = "https://c90d809b75da1f7771108dedcb78e9be@o4508410137411584.ingest.us.sentry.io/4511259248492544";
                    options.Debug = false;
                    options.AutoSessionTracking = true;
                    options.CaptureFailedRequests = true;
                })
                .UseMauiCommunityToolkitCamera()
                .UseMauiCommunityToolkit()
                .ConfigureSyncfusionToolkit()
                .ConfigureSyncfusionCore()
                .ConfigureMauiHandlers(handlers =>
                {
#if IOS || MACCATALYST
    				handlers.AddHandler<Microsoft.Maui.Controls.CollectionView, Microsoft.Maui.Controls.Handlers.Items2.CollectionViewHandler2>();
#endif
                }).UseLocalizationResourceManager(settings =>
                {
                    settings.AddResource(Traducao.ResourceManager);
                    settings.RestoreLatestCulture(true);
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                    fonts.AddFont("NunitoSans-Regular.ttf", "Nunito");

                    fonts.AddFont("FontAwesomeBrands.otf", "FontAwesomeBrands");
                    fonts.AddFont("FontAwesomeLight.otf", "FontAwesomeLight");
                    fonts.AddFont("FontAwesomeRegular.otf", "FontAwesomeRegular");
                    fonts.AddFont("FontAwesomeSolid.otf", "FontAwesomeSolid");
                });

#if DEBUG
            builder.Logging.AddDebug();
            builder.Services.AddLogging(configure => configure.AddDebug());
#endif

            // Usa implementação nativa MAUI para resize de imagens
            builder.Services.AddSingleton<IResizeImageCommand, SkiaSharpResizeCommand>();

            builder.Services.AddSingleton<CacheService>();

            builder.Services.AddSingleton<HtmlErrorPopup>();

            builder.Services.AddTransient<SuportePage>();

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddTransient<AvaliacaoAlternativas>();

            builder.Services.AddSingleton<ISIWebService>();


            #region Registra Views

            //Atividades
            builder.Services.AddTransient<AtividadeView>();
            builder.Services.AddTransient<AtividadeViewModel>();

            //Notificações
            builder.Services.AddTransient<NotificacoesView>();


            //Minha Conta
            builder.Services.AddTransient<MinhaConta>();
            builder.Services.AddTransient<MinhaContaViewModel>();

            //Regional
            builder.Services.AddTransient<RegionalView>();
            builder.Services.AddTransient<RegionalView_Edit>();
            builder.Services.AddTransient<RegionalEditViewModel>();

            //Propriedade
            builder.Services.AddTransient<PropriedadeView>();
            builder.Services.AddTransient<PropriedadeView_Edit>();
            builder.Services.AddTransient<PropriedadeEditViewModel>();

            //Unidade Epidemiologica
            builder.Services.AddTransient<UnidadeEpidemiologicaView>();
            builder.Services.AddTransient<UnidadeEpidemiologicaView_Edit>();
            builder.Services.AddTransient<UnidadeEpidemiologicaEditViewModel>();

            //Proprietário
            builder.Services.AddTransient<ProprietarioView>();
            builder.Services.AddTransient<ProprietarioView_Edit>();
            builder.Services.AddTransient<ProprietarioEditViewModel>();

            //Login
            builder.Services.AddTransient<Login>();
            builder.Services.AddTransient<LoginViewModel>();

            //Dashboard
            builder.Services.AddSingleton<DashboardView>();
            builder.Services.AddSingleton<DashboardViewModel>();
            builder.Services.AddSingleton<MercadosDashboardViewModel>();
            builder.Services.AddSingleton<GalpoesViewModel>();
            builder.Services.AddSingleton<GalpoesView>();

            //Gráficos
            builder.Services.AddSingleton<GraficoResultadosPage>();
            builder.Services.AddSingleton<GraficoResultadosViewModel>();


            //Sincronizacao
            builder.Services.AddTransient<SincronizacaoPageModal>();
            builder.Services.AddSingleton<SincronizacaoPendentesViewModel>();

            //PopUp Usuário
            builder.Services.AddSingleton<PopUpUsuario>();
            builder.Services.AddSingleton<PopUpUsuarioViewModel>();

            //Lote
            builder.Services.AddTransient<LoteEditView>();
            builder.Services.AddTransient<LoteEditViewModel>();

            //Lote Monitoramento (View transient para evitar reuso visual no iOS; ViewModel singleton para manter o fluxo atual de SetInitialState)
            builder.Services.AddTransient<LoteMonitoramentoView>();
            builder.Services.AddSingleton<LoteMonitoramentoViewModel>();

            //LoteView Formulário (View transient para evitar reuso de Page/handler no iOS)
            builder.Services.AddSingleton<LoteFormularioViewModel>();
            builder.Services.AddTransient<LoteFormularioView>();

            //Avaliação no Galpão — página dedicada (evita conflitos com formulários normais)
            builder.Services.AddSingleton<AvaliacaoGalpaoFormViewModel>();
            builder.Services.AddTransient<AvaliacaoGalpaoFormView>();

            //ISI Macro (Transient: evita reuso de visual tree e estado de lista no iOS)
            builder.Services.AddTransient<LoteISIMacroView>();
            builder.Services.AddTransient<LoteISIMacroViewModel>();
            builder.Services.AddTransient<SelectModeloPopup>();
            builder.Services.AddTransient<ISIMacroNotaSelecionaImagem>();
            builder.Services.AddTransient<ISIMacroNotaSelecionaImagemViewModel>();

            //Tratamento
            builder.Services.AddTransient<LoteTratamentoView>();
            builder.Services.AddTransient<LoteTratamentoViewModel>();

            //Manejo
            builder.Services.AddTransient<LoteManejoView>();
            builder.Services.AddTransient<LoteManejoViewModel>();

            //Zootenico
            builder.Services.AddTransient<LoteZootecnicoView>();
            builder.Services.AddTransient<LoteZootecnicoViewModel>();

            #endregion

            builder.Services.AddSingleton<SyncService>();
            builder.Services.AddSingleton<HomeService>();
            builder.Services.AddSingleton<GraficosService>();
            builder.Services.AddTransient<SincronizacaoView>();
            builder.Services.AddTransient<SincronizacaoViewModel>();


            #region Registra ViewModels


            //Regista ViewModels
            builder.Services.AddSingleton<AvaliacaoAlternativasViewModel>();
            builder.Services.AddSingleton<MainPageModel>();
            builder.Services.AddSingleton<LoteViewModel>();
            builder.Services.AddSingleton<HomeViewModel>();
            builder.Services.AddTransient<SuportePageViewModel>();
            builder.Services.AddTransient<ConfigViewModel>();


            builder.Services.AddSingleton<LoteAvaliacaoGalpaoViewModel>();
            builder.Services.AddTransient<LoteDiagnosticoViewModel>();
            builder.Services.AddTransient<LoteISIMicroViewModel>();
            
            builder.Services.AddTransient<LoteNutricaoViewModel>();
            builder.Services.AddTransient<LoteSalmonellaViewModel>();
            builder.Services.AddTransient<LoteVacinasViewModel>();

            #endregion



            try
            {
                return builder.Build();
            }
            catch (Exception ex)
            {
                // Log detailed error so we can see the cause in device logs (Console / Xcode)
                System.Diagnostics.Debug.WriteLine($"[MauiProgram] Falha ao criar app: {ex}");
                System.Console.WriteLine($"[MauiProgram] Falha ao criar app: {ex}");

                // Retorna um app mínimo de fallback para evitar crash imediato e permitir leitura dos logs.
                var fallbackBuilder = MauiApp.CreateBuilder()
                    .UseMauiApp<MinimalFallbackApp>();

                var fallbackApp = fallbackBuilder.Build();
                return fallbackApp;
            }
        }
    }
}
