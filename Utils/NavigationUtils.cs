using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;

using SilvaData.Infrastructure;
using SilvaData.Models;
using SilvaData.Pages.PopUps;

using Microsoft.Extensions.DependencyInjection;

using System.Diagnostics;
using System.Globalization;

namespace SilvaData.Utils
{
    /// <summary>
    /// Fornece métodos utilitários para navegação e gerenciamento de páginas modais e popups em .NET MAUI.
    /// Esta classe garante que as operações de UI sejam executadas na thread principal e de forma segura.
    /// </summary>
    public static class NavigationUtils
    {
        private static readonly SemaphoreSlim _modalSemaphore = new(1, 1);

        // ✅ NOVO: Tracking de páginas com modal aberta
        private static readonly HashSet<WeakReference<Page>> _paginasComModalAberta = new();
        private static readonly object _trackingLock = new();

        /// <summary>
        /// Obtém de forma segura a instância de navegação da página principal da aplicação.
        /// </summary>
        private static INavigation? Navigation => Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;

        private static void WriteDiagnosticLine(string line)
        {
            Debug.WriteLine(line);
            Console.WriteLine(line);
        }

        private static void LogNavigation(string message)
        {
            WriteDiagnosticLine($"[NavigationUtils] {DateTime.Now:HH:mm:ss.fff} | {GetThreadContext()} | tracked={GetTrackedPageCount()} | modalStack={DescribeModalStack()} | {message}");
        }

        private static void LogNavigationError(string origin, Exception ex)
        {
            WriteDiagnosticLine($"[NavigationUtils.{origin}] {DateTime.Now:HH:mm:ss.fff} | {GetThreadContext()} | tracked={GetTrackedPageCount()} | modalStack={DescribeModalStack()} | erro={ex.Message}");
        }

        public static void LogExternal(string source, string message)
        {
            WriteDiagnosticLine($"[{source}] {DateTime.Now:HH:mm:ss.fff} | {GetThreadContext()} | modalStack={DescribeModalStack()} | {message}");
        }

        private static string GetThreadContext()
        {
            var taskId = Task.CurrentId?.ToString(CultureInfo.InvariantCulture) ?? "-";
            return $"thread={Environment.CurrentManagedThreadId} | main={(MainThread.IsMainThread ? "yes" : "no")} | task={taskId}";
        }

        private static int GetTrackedPageCount()
        {
            lock (_trackingLock)
            {
                LimparReferenciasInvalidas();
                return _paginasComModalAberta.Count;
            }
        }

        private static string DescribeModalStack()
        {
            var modalStack = Navigation?.ModalStack;
            if (modalStack == null || modalStack.Count == 0)
                return "0:[]";

            return $"{modalStack.Count}:[{string.Join(" > ", modalStack.Select(DescribePage))}]";
        }

        #region Tracking de Modal (NOVO)

        /// <summary>
        /// ✅ Marca que uma página está abrindo um modal.
        /// </summary>
        private static void MarcarModalAberta(Page? pagina)
        {
            if (pagina == null) return;

            lock (_trackingLock)
            {
                try
                {
                    // Remove referências mortas
                    LimparReferenciasInvalidas();

                    // Evita duplicatas - verifica se já está marcada
                    if (_paginasComModalAberta.Any(wr => wr.TryGetTarget(out var p) && p == pagina))
                    {
                        LogNavigation($"Tracking já registrado: {DescribePage(pagina)}");
                        return;
                    }

                    _paginasComModalAberta.Add(new WeakReference<Page>(pagina));
                    LogNavigation($"Tracking registrado: {DescribePage(pagina)}");
                }
                catch (Exception ex)
                {
                    LogNavigationError(nameof(MarcarModalAberta), ex);
                }
            }
        }

        /// <summary>
        /// ✅ Desmarca que uma página tinha modal aberta.
        /// </summary>
        private static void DesmarcarModalAberta(Page? pagina)
        {
            if (pagina == null) return;

            lock (_trackingLock)
            {
                try
                {
                    var toRemove = _paginasComModalAberta
                        .Where(wr => wr.TryGetTarget(out var p) && p == pagina)
                        .ToList();

                    foreach (var wr in toRemove)
                    {
                        _paginasComModalAberta.Remove(wr);
                    }

                    if (toRemove.Count > 0)
                    {
                        LogNavigation($"Tracking removido: {DescribePage(pagina)} | removidos: {toRemove.Count}");
                    }
                    else
                    {
                        LogNavigation($"Tracking não encontrado para remover: {DescribePage(pagina)}");
                    }

                    // Aproveita e limpa referências mortas
                    LimparReferenciasInvalidas();
                }
                catch (Exception ex)
                {
                    LogNavigationError(nameof(DesmarcarModalAberta), ex);
                }
            }
        }

        /// <summary>
        /// ✅ Verifica se uma página específica tem modal aberta.
        /// </summary>
        public static bool TemModalAberta(Page pagina)
        {
            if (pagina == null) 
                return false;

            lock (_trackingLock)
            {
                try
                {
                    LimparReferenciasInvalidas();

                    // ✅ Verificação mais robusta: confirma que a página ainda está na navigation stack
                    var currentPage = GetCurrentPage();
                    if (currentPage == null)
                        return false;

                    // Verifica se a página informada é a página atual ou está na modal stack
                    bool isCurrentPage = currentPage == pagina;
                    bool hasModalOpen = Navigation?.ModalStack?.Count > 0;

                    return _paginasComModalAberta.Any(wr =>
                        wr.TryGetTarget(out var p) && p == pagina) && (isCurrentPage || hasModalOpen);
                }
                catch (Exception ex)
                {
                    LogNavigationError(nameof(TemModalAberta), ex);
                    return false; // Em caso de erro, assume que não tem modal aberta
                }
            }
        }

        /// <summary>
        /// ✅ Remove referências fracas que não têm mais target.
        /// </summary>
        private static void LimparReferenciasInvalidas()
        {
            var invalidas = _paginasComModalAberta
                .Where(wr => !wr.TryGetTarget(out _))
                .ToList();

            foreach (var wr in invalidas)
            {
                _paginasComModalAberta.Remove(wr);
            }
        }

        #endregion

        #region Gerenciamento de Páginas Modais

        /// <summary>
        /// Exibe uma página modal, garantindo que não haja outra instância do mesmo tipo já aberta.
        /// </summary>
        private static async Task ShowModalAsync(Page page, bool animated = true)
        {
            ArgumentNullException.ThrowIfNull(page);

            try
            {
                LogNavigation($"ShowModalAsync iniciado | destino={DescribePage(page)} | animated={animated}");

                if (IsModalDisplayed(page.GetType()) || Navigation is null)
                {
                    LogNavigation($"ShowModalAsync ignorado | destino={DescribePage(page)} | jaExibido={IsModalDisplayed(page.GetType())} | navigationNull={Navigation is null}");
                    return;
                }

                LogNavigation($"Aguardando _modalSemaphore para abrir modal | destino={DescribePage(page)}");
                await _modalSemaphore.WaitAsync().ConfigureAwait(false);
                LogNavigation($"_modalSemaphore adquirido para abrir modal | destino={DescribePage(page)}");
                
                try
                {
                    if (IsModalDisplayed(page.GetType()))
                    {
                        LogNavigation($"ShowModalAsync cancelado apos lock | destino={DescribePage(page)} ja estava aberto");
                        return;
                    }

                    // ✅ Marca página chamadora ANTES de abrir modal
                    var paginaChamadora = GetCurrentPage();
                    var origem = DescribePage(paginaChamadora);
                    var destino = DescribePage(page);
                    MarcarModalAberta(paginaChamadora);
                    LogNavigation($"Preparando InvokeOnMainThreadAsync para abrir modal | destino={destino} | origem={origem}");

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        try
                        {
                            LogNavigation($"Entrou na main thread para abrir modal | destino={destino} | origem={origem}");

                            // StatusBarBehavior desabilitado temporariamente para diagnóstico de crash na abertura modal.

                            LogNavigation($"Chamando PushModalAsync | destino={destino} | origem={origem}");
                            var pushStopwatch = Stopwatch.StartNew();
                            var pushTask = Navigation.PushModalAsync(page, animated);
                            var completed = await Task.WhenAny(pushTask, Task.Delay(12000));

                            if (completed != pushTask)
                            {
                                LogNavigation($"PushModalAsync timeout após {pushStopwatch.ElapsedMilliseconds}ms | destino={destino} | origem={origem}");
                                return;
                            }

                            await pushTask;
                            LogNavigation($"Modal aberta: {destino} | origem: {origem} | ms={pushStopwatch.ElapsedMilliseconds}");
                        }
                        catch (Exception ex)
                        {
                            LogNavigationError(nameof(ShowModalAsync), ex);
                            // Desmarca em caso de falha
                            DesmarcarModalAberta(paginaChamadora);
                            throw;
                        }
                    }).ConfigureAwait(false);
                }
                finally
                {
                    _modalSemaphore.Release();
                    LogNavigation($"_modalSemaphore liberado apos abertura modal | destino={DescribePage(page)}");
                }
            }
            catch (Exception ex)
            {
                LogNavigationError(nameof(ShowModalAsync), ex);
            }
        }

        /// <summary>
        /// Fecha a página modal mais recente na pilha de navegação.
        /// </summary>
        public static async Task PopModalAsync(bool animated = true)
        {
            if (Navigation?.ModalStack?.Count is null or 0)
            {
                LogNavigation($"PopModalAsync ignorado | nenhuma modal aberta | animated={animated}");
                return;
            }

            LogNavigation($"Aguardando _modalSemaphore para fechar modal | animated={animated}");
            await _modalSemaphore.WaitAsync().ConfigureAwait(false);
            LogNavigation($"_modalSemaphore adquirido para fechar modal | animated={animated}");
            try
            {
                var page = Navigation.ModalStack.LastOrDefault();

                // ✅ Identifica página que vai receber foco de volta ANTES de fechar
                var paginaQueVaiVoltar = GetCurrentPageBeforeModal();
                var modalFechado = DescribePage(page);
                var retorno = DescribePage(paginaQueVaiVoltar);

                if (page != null)
                {
                    LogNavigation($"Preparando InvokeOnMainThreadAsync para fechar modal | modal={modalFechado} | retorno={retorno}");
                    await MainThread.InvokeOnMainThreadAsync(() => Navigation.PopModalAsync(animated)).ConfigureAwait(false);
                    LogNavigation($"Modal fechada: {modalFechado} | retornando para: {retorno}");

                    if (page is IDisposable d)
                    {
                        try { d.Dispose(); }
                        catch (Exception ex) { LogNavigationError(nameof(PopModalAsync), ex); }
                    }
                }

                // ✅ Desmarca modal IMEDIATAMENTE (sem delay para evitar race condition)
                DesmarcarModalAberta(paginaQueVaiVoltar);
            }
            finally
            {
                _modalSemaphore.Release();
                LogNavigation("_modalSemaphore liberado apos fechamento modal");
            }
        }

        /// <summary>
        /// Fecha todas as páginas modais abertas na pilha de navegação.
        /// </summary>
        public static async Task CloseAllModalsAsync(bool animated = true)
        {
            if (Navigation?.ModalStack?.Count is null or 0)
            {
                LogNavigation($"CloseAllModalsAsync ignorado | nenhuma modal aberta | animated={animated}");
                return;
            }

            LogNavigation($"Aguardando _modalSemaphore para fechar todas as modais | animated={animated}");
            await _modalSemaphore.WaitAsync().ConfigureAwait(false);
            LogNavigation($"_modalSemaphore adquirido para fechar todas as modais | animated={animated}");
            try
            {
                while (Navigation.ModalStack.Count > 0)
                {
                    var modalAtual = Navigation.ModalStack.LastOrDefault();
                    var paginaQueVaiVoltar = GetCurrentPageBeforeModal();
                    var modalFechado = DescribePage(modalAtual);
                    var retorno = DescribePage(paginaQueVaiVoltar);
                    bool shouldAnimate = animated && Navigation.ModalStack.Count == 1;
                    LogNavigation($"Fechando modal em lote | modal={modalFechado} | retorno={retorno} | animated={shouldAnimate}");
                    await MainThread.InvokeOnMainThreadAsync(() => Navigation.PopModalAsync(shouldAnimate)).ConfigureAwait(false);
                    LogNavigation($"Modal fechada: {modalFechado} | retornando para: {retorno}");

                    if (modalAtual is IDisposable d)
                    {
                        try { d.Dispose(); }
                        catch (Exception ex) { LogNavigationError(nameof(CloseAllModalsAsync), ex); }
                    }

                    DesmarcarModalAberta(paginaQueVaiVoltar);
                }
            }
            finally
            {
                _modalSemaphore.Release();
                LogNavigation("_modalSemaphore liberado apos CloseAllModalsAsync");
            }
        }

        /// <summary>
        /// Verifica se uma página modal de um tipo específico está atualmente exibida.
        /// </summary>
        public static bool IsModalDisplayed(Type pageType)
        {
            return Navigation?.ModalStack?.Any(p => p.GetType() == pageType) ?? false;
        }

        #endregion

        #region Gerenciamento de Popups (CommunityToolkit)

        /// <summary>
        /// Exibe um Popup (não genérico) do CommunityToolkit.Maui na página atual.
        /// Este é um wrapper para o método não-genérico que retorna 'object?'.
        /// </summary>
        public static async Task ShowPopupAsync(Popup popup)
        {
            ArgumentNullException.ThrowIfNull(popup);

            Page? currentPage = GetCurrentPage();
            if (currentPage is null)
            {
                LogNavigation("ShowPopupAsync cancelado | pagina atual nao encontrada");
                return;
            }

            try
            {
                // ✅ Marca antes de abrir popup
                var popupName = DescribePopup(popup);
                var origem = DescribePage(currentPage);
                MarcarModalAberta(currentPage);
                LogNavigation($"Popup aberta: {popupName} | origem: {origem}");

                LogNavigation($"Preparando InvokeOnMainThreadAsync para popup | popup={popupName} | origem={origem}");
                await MainThread.InvokeOnMainThreadAsync(() => currentPage.ShowPopupAsync(popup)).ConfigureAwait(false);
                LogNavigation($"Popup fechada: {popupName} | retornando para: {origem}");

                // ✅ Desmarca imediatamente (sem delay para evitar race condition)
                DesmarcarModalAberta(currentPage);
            }
            catch (Exception ex)
            {
                DesmarcarModalAberta(currentPage); // ✅ Garante limpeza em caso de erro
                LogNavigationError(nameof(ShowPopupAsync), ex);
            }
        }

        /// <summary>
        /// Exibe um Popup do CommunityToolkit.Maui de forma segura na thread da UI.
        /// </summary>
        public static async Task<TResult?> ShowPopupAsync<TResult>(Popup popup)
        {
            ArgumentNullException.ThrowIfNull(popup);

            var currentPage = GetCurrentPage();
            if (currentPage is null)
            {
                LogNavigation("ShowPopupAsync<TResult> cancelado | pagina atual nao encontrada");
                return default;
            }

            try
            {
                // ✅ Marca antes de abrir
                var popupName = DescribePopup(popup);
                var origem = DescribePage(currentPage);
                MarcarModalAberta(currentPage);
                LogNavigation($"Popup aberta: {popupName} | origem: {origem}");

                TResult? result = default;

                // CASO 1: Popup genérico (Popup<TResult>)
                if (popup is Popup<TResult> typedPopup)
                {
                    LogNavigation($"Preparando popup tipado | popup={popupName} | resultado={typeof(TResult).Name}");
                    var popupResult = await MainThread.InvokeOnMainThreadAsync(() =>
                        currentPage.ShowPopupAsync<TResult>(typedPopup)
                    ).ConfigureAwait(false);

                    if (popupResult is null || popupResult.WasDismissedByTappingOutsideOfPopup)
                        result = default;
                    else
                        result = popupResult.Result;
                }
                // CASO 2: Popup não-genérico (Popup)
                else
                {
                    LogNavigation($"Preparando popup nao tipado | popup={popupName} | resultadoEsperado={typeof(TResult).Name}");
                    var objectResult = await MainThread.InvokeOnMainThreadAsync(() =>
                        currentPage.ShowPopupAsync(popup)
                    ).ConfigureAwait(false);

                    if (objectResult is null)
                    {
                        LogNavigation($"Popup fechado sem resultado | popup={popupName}");
                        result = default;
                    }
                    else if (objectResult is TResult typedResult)
                    {
                        result = typedResult;
                    }
                    else
                    {
                        try
                        {
                            result = (TResult)Convert.ChangeType(objectResult, typeof(TResult), CultureInfo.InvariantCulture);
                        }
                        catch (Exception ex)
                        {
                            LogNavigation($"Popup com resultado incompatível | popup={popupName} | esperado={typeof(TResult).Name} | recebido={objectResult.GetType().Name} | erro={ex.Message}");
                            result = default;
                        }
                    }
                }

                // ✅ Desmarca imediatamente (sem delay para evitar race condition)
                DesmarcarModalAberta(currentPage);
                LogNavigation($"Popup fechada: {popupName} | retornando para: {origem} | resultado={(result is null ? "<null>" : result)}");

                return result;
            }
            catch (Exception ex)
            {
                DesmarcarModalAberta(currentPage); // ✅ Garante limpeza
                LogNavigationError("ShowPopupAsync<TResult>", ex);
                return default;
            }
        }

        #endregion

        #region Helpers de UI (Métodos Implementados)

        /// <summary>
        /// (Restaurado do Helpers.cs)
        /// Exibe um PopUp "Sim/Não" perguntando se o usuário deseja sair.
        /// Se sim, fecha a página modal atual.
        /// </summary>
        public static async Task<bool> PopModalSeConfirmar()
        {
            var result = await PopUpYesNo.ShowAsync(Traducao.confirmacao, Traducao.sairformulario).ConfigureAwait(false);

            if (!result) return false;

            await PopModalAsync().ConfigureAwait(false);
            return true;
        }

        #endregion

        #region Métodos auxiliares de contexto de página

        /// <summary>
        /// Obtém a página atualmente visível para o usuário.
        /// </summary>
        private static Page? GetCurrentPage()
        {
            var mainPage = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (mainPage is null)
            {
                return null;
            }

            // Verifica se há modais abertos
            if (mainPage.Navigation.ModalStack.Count > 0)
            {
                return FindCurrentPage(mainPage.Navigation.ModalStack.Last());
            }

            // Se não houver modais, busca na página principal
            return FindCurrentPage(mainPage);
        }

        /// <summary>
        /// ✅ NOVO: Obtém a página que vai receber foco quando modal fechar.
        /// </summary>
        private static Page? GetCurrentPageBeforeModal()
        {
            var mainPage = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (mainPage is null) return null;

            // Se tem apenas 1 modal, vai voltar para página principal
            if (mainPage.Navigation.ModalStack.Count == 1)
            {
                return FindCurrentPage(mainPage);
            }

            // Se tem múltiplos modais, vai voltar para o penúltimo
            if (mainPage.Navigation.ModalStack.Count > 1)
            {
                return FindCurrentPage(mainPage.Navigation.ModalStack[^2]);
            }

            return FindCurrentPage(mainPage);
        }

        /// <summary>
        /// Navega recursivamente por Shell, NavigationPage ou TabbedPage
        /// para encontrar a página que está realmente visível.
        /// </summary>
        private static Page FindCurrentPage(Page page)
        {
            return page switch
            {
                Shell shell => FindCurrentPage(shell.CurrentPage),
                NavigationPage navPage => FindCurrentPage(navPage.CurrentPage),
                TabbedPage tabbedPage => FindCurrentPage(tabbedPage.CurrentPage),
                FlyoutPage flyoutPage => FindCurrentPage(flyoutPage.Detail),
                _ => page
            };
        }

        private static string DescribePage(Page? page)
        {
            if (page == null)
                return "<desconhecida>";

            if (page is ContentPage contentPage && contentPage.Content != null)
            {
                var pageTypeName = contentPage.GetType().Name;
                var contentTypeName = contentPage.Content.GetType().Name;

                if (pageTypeName == nameof(ContentPage) && contentTypeName != nameof(ContentPage))
                    return $"{pageTypeName}({contentTypeName})";
            }

            return page.GetType().Name;
        }

        private static string DescribePopup(Popup? popup)
        {
            return popup?.GetType().Name ?? "<popup-desconhecido>";
        }

        #endregion

        #region Métodos de Resolução de View (DI)

        /// <summary>
        /// Resolve e exibe um tipo de view registrado no contêiner de DI como uma página modal.
        /// </summary>
        public static async Task ShowViewAsModalAsync<TView>() where TView : class
        {
            var services = ServiceHelper.Services;
            if (services is null)
            {
                LogNavigation($"ShowViewAsModalAsync cancelado | IServiceProvider nulo | tipo={typeof(TView).FullName}");
                return;
            }

            TView? view;
            try
            {
                // ✅ Tenta resolver do DI (Singleton/Transient/Scoped)
                view = services.GetService<TView>();
                
                // Se não está registrado, tenta criar com ActivatorUtilities (injeta dependências automaticamente)
                if (view is null)
                {
                    LogNavigation($"View nao registrada no DI | tipo={typeof(TView).Name} | usando ActivatorUtilities");
                    view = ActivatorUtilities.CreateInstance<TView>(services);
                }

                LogNavigation($"View resolvida para modal | solicitada={typeof(TView).Name} | resolvida={view?.GetType().Name}");
            }
            catch (Exception ex)
            {
                LogNavigation($"Falha ao resolver view via DI | tipo={typeof(TView).FullName} | excecao={ex}");
                await PopUpOK.ShowAsync("Erro de Navegação", $"Não foi possível resolver a view: {typeof(TView).Name}. Verifique o registro de DI.").ConfigureAwait(false);
                return;
            }

            if (view is null)
            {
                LogNavigation($"ShowViewAsModalAsync cancelado | view resolvida nula | tipo={typeof(TView).FullName}");
                return;
            }

            await ShowViewAsModalInternalAsync(view).ConfigureAwait(false);
        }

        /// <summary>
        /// Resolve e exibe uma view como modal usando DI + argumentos customizados de construtor.
        /// Os serviços registrados no DI são injetados automaticamente, os args são para parâmetros adicionais.
        /// </summary>
        public static async Task ShowViewAsModalAsync<TView>(params object[] args) where TView : class
        {
            var services = ServiceHelper.Services;
            if (services is null)
            {
                LogNavigation($"ShowViewAsModalAsync(args) cancelado | IServiceProvider nulo | tipo={typeof(TView).FullName}");
                return;
            }

            TView? view;
            try
            {
                // ✅ ESTRATÉGIA HÍBRIDA:
                // 1. Se está registrado como Singleton/Scoped, usa a instância existente
                // 2. Se é Transient ou não registrado, cria nova instância injetando DI + args
                
                var descriptor = FindServiceDescriptor<TView>(services);
                
                if (descriptor?.Lifetime == ServiceLifetime.Singleton)
                {
                    // Para Singleton, SEMPRE usa a instância existente (ignora args)
                    view = services.GetRequiredService<TView>();
                    LogNavigation($"Usando Singleton existente para modal | tipo={typeof(TView).Name} | args={args.Length}");
                }
                else
                {
                    // Para Transient/Scoped ou não-registrado: cria nova instância com DI + args
                    view = ActivatorUtilities.CreateInstance<TView>(services, args);
                    LogNavigation($"Criada nova instancia com args para modal | tipo={typeof(TView).Name} | args={args.Length}");
                }
            }
            catch (Exception ex)
            {
                LogNavigation($"Falha ao criar view com args | tipo={typeof(TView).FullName} | excecao={ex}");
                await PopUpOK.ShowAsync("Erro de Navegação", $"Falha ao criar {typeof(TView).Name}: {ex.Message}").ConfigureAwait(false);
                return;
            }

            if (view is null)
            {
                LogNavigation($"ShowViewAsModalAsync(args) cancelado | view criada nula | tipo={typeof(TView).FullName}");
                return;
            }

            await ShowViewAsModalInternalAsync(view).ConfigureAwait(false);
        }

        /// <summary>
        /// ✅ NOVO: Helper para descobrir se um tipo está registrado no DI e seu lifetime.
        /// </summary>
        private static ServiceDescriptor? FindServiceDescriptor<TView>(IServiceProvider services)
        {
            try
            {
                // Acessa a coleção de descritores (se disponível via reflexão ou cast)
                if (services is ServiceProvider sp)
                {
                    var engineField = sp.GetType().GetField("_engine", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (engineField?.GetValue(sp) is object engine)
                    {
                        var descriptorsProperty = engine.GetType().GetProperty("CallSiteFactory", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (descriptorsProperty?.GetValue(engine) is object factory)
                        {
                            var descriptorsProp = factory.GetType().GetProperty("Descriptors", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                            if (descriptorsProp?.GetValue(factory) is IEnumerable<ServiceDescriptor> descriptors)
                            {
                                return descriptors.FirstOrDefault(d => d.ServiceType == typeof(TView));
                            }
                        }
                    }
                }
            }
            catch
            {
                // Reflexão falhou, assume que não está registrado ou é Transient
            }

            return null;
        }

        /// <summary>
        /// Lógica interna para exibir uma view (Page ou View) como modal.
        /// </summary>
        private static async Task ShowViewAsModalInternalAsync(object view)
        {
            if (view is Page page)
            {
                LogNavigation($"ShowViewAsModalInternalAsync com Page | view={DescribePage(page)}");
                await ShowModalAsync(page).ConfigureAwait(false);
                return;
            }

            if (view is View contentView)
            {
                var wrapper = new ContentPage { Content = contentView };
                LogNavigation($"ShowViewAsModalInternalAsync com View | view={contentView.GetType().Name} | wrapper={DescribePage(wrapper)}");
                await ShowModalAsync(wrapper).ConfigureAwait(false);
                return;
            }

            LogNavigation($"Tipo de view nao suportado para modal | tipo={view.GetType().FullName}");
        }

        #endregion

        #region Métodos utilitários para abertura de formulários (NOVO)

        /// <summary>
        /// Abre o LoteFormularioView já configurando o LoteFormularioViewModel via DI em uma única operação.
        /// Evita race conditions com mensagens. Retorna false em caso de erro.
        /// </summary>
        public static async Task<bool> OpenLoteFormularioAsync(
            Lote lote,
            int loteFormId,
            int parametroTipoId,
            int? fase,
            bool isReadOnly,
            bool podeEditar,
            int? item = null,
            int? modeloIsiMacroSelecionado = null,
            bool limpaFormularioAtual = false,
            Parametro? parametroSelecionado = null,
            LoteFormulario? recoveredForm = null,
            int loteFormVinculado = -1)
        {
            // Avaliação no Galpão tem fluxo dedicado: SetInitialState antes do push
            // para que IsBusy=true e ParametroSelecionado estejam corretos no primeiro render.
            if (parametroTipoId == 20)
            {
                return await OpenAvaliacaoGalpaoFormAsync(
                    lote, loteFormId, fase, isReadOnly, podeEditar,
                    limpaFormularioAtual, parametroSelecionado);
            }

            try
            {
                ArgumentNullException.ThrowIfNull(lote);
                var loteAtual = lote;

                var services = ServiceHelper.Services;
                if (services is null)
                {
                    LogNavigation("OpenLoteFormularioAsync cancelado | IServiceProvider nulo");
                    await PopUpOK.ShowAsync("Erro", "Serviços indisponíveis");
                    return false;
                }

                LogNavigation($"OpenLoteFormularioAsync iniciado | lote={loteAtual.id} | loteFormId={loteFormId} | parametroTipoId={parametroTipoId} | fase={fase} | readOnly={isReadOnly} | podeEditar={podeEditar} | item={item} | modeloIsiMacroSelecionado={modeloIsiMacroSelecionado} | limpaFormularioAtual={limpaFormularioAtual} | recoveredForm={(recoveredForm != null ? "yes" : "no")} | loteFormVinculado={loteFormVinculado}");

                var vm = services.GetRequiredService<SilvaData.ViewModels.LoteFormularioViewModel>();

                if (limpaFormularioAtual)
                    vm.Cleanup();

                // Configura estado ANTES de abrir a modal para evitar race condition:
                // OnAppearing dispara Carregar() que lê esses params, e deve encontrá-los já corretos.
                LogNavigation($"OpenLoteFormularioAsync configurando estado antes de abrir modal | loteFormId={loteFormId} | parametroTipoId={parametroTipoId}");
                vm.SetInitialState(
                    lote: loteAtual,
                    loteFormId: loteFormId,
                    parametroTipoId: parametroTipoId,
                    fase: fase,
                    isReadOnly: isReadOnly,
                    podeEditar: podeEditar,
                    recoveredForm: recoveredForm,
                    loteFormVinculado: loteFormVinculado);

                if (item.HasValue)
                    vm.Item = item;

                if (parametroSelecionado != null)
                    vm.ParametroSelecionado = parametroSelecionado;

                vm.SetPendingLoadParams(limpaFormularioAtual, modeloIsiMacroSelecionado);
                vm.IsBusy = true;
                vm.MostrarBotaoCarregar = false;

                LogNavigation("OpenLoteFormularioAsync estado configurado, abrindo modal");

                SilvaData.Controls.LoteFormularioView? formPage = null;

                if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    LogNavigation("OpenLoteFormularioAsync aguardando estabilizacao iOS (350ms) antes de abrir modal");
                    await Task.Delay(350);

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        LogNavigation("OpenLoteFormularioAsync [iOS-inner] iniciando CreateInstance");
                        formPage = ActivatorUtilities.CreateInstance<SilvaData.Controls.LoteFormularioView>(services);
                        LogNavigation($"OpenLoteFormularioAsync [iOS-inner] CreateInstance concluido | hash={formPage.GetHashCode()}");

                        var nav = Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;
                        if (nav == null)
                            throw new InvalidOperationException("Navigation indisponível para PushModalAsync direto no iOS.");

                        LogNavigation($"OpenLoteFormularioAsync [iOS-inner] chamando PushModalAsync | modalStack={nav.ModalStack.Count}");
                        await nav.PushModalAsync(formPage, false);
                        LogNavigation("OpenLoteFormularioAsync [iOS-inner] PushModalAsync concluido");
                    });

                    // Cede um frame para a tela concluir o primeiro layout antes de continuar.
                    await Task.Yield();
                    LogNavigation("OpenLoteFormularioAsync modal iOS aberta");
                }
                else
                {
                    LogNavigation("OpenLoteFormularioAsync abrindo modal padrão para LoteFormularioView");
                    await ShowViewAsModalAsync<SilvaData.Controls.LoteFormularioView>();
                    LogNavigation("OpenLoteFormularioAsync modal padrão aberta");
                }

                LogNavigation("OpenLoteFormularioAsync params salvos; carregamento sera disparado apos OnAppearing da View");

                return true;
            }
            catch (Exception ex)
            {
                LogNavigationError(nameof(OpenLoteFormularioAsync), ex);
                await PopUpOK.ShowAsync("Erro", $"Falha ao abrir formulário: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Abre a página dedicada de Avaliação no Galpão.
        /// SetInitialState e IsBusy=true são definidos ANTES do PushModalAsync para que
        /// o primeiro render já exiba o loading e o ParametroSelecionado correto.
        /// </summary>
        private static async Task<bool> OpenAvaliacaoGalpaoFormAsync(
            Lote lote,
            int loteFormId,
            int? fase,
            bool isReadOnly,
            bool podeEditar,
            bool limpaFormularioAtual,
            SilvaData.Models.Parametro? parametroSelecionado)
        {
            try
            {
                var services = ServiceHelper.Services;
                if (services is null)
                {
                    await PopUpOK.ShowAsync("Erro", "Serviços indisponíveis");
                    return false;
                }

                var vm = services.GetRequiredService<SilvaData.ViewModels.AvaliacaoGalpaoFormViewModel>();

                if (limpaFormularioAtual)
                    vm.Cleanup();

                // Define estado ANTES do push: IsBusy=true e ParametroSelecionado visíveis
                // no primeiro render da página (sem precisar de notificação).
                vm.SetInitialState(lote, loteFormId, fase, isReadOnly, podeEditar, parametroSelecionado);
                vm.IsBusy = true;

                LogNavigation($"OpenAvaliacaoGalpaoFormAsync | lote={lote.id} | loteFormId={loteFormId} | param={parametroSelecionado?.nome} | Qualitativo={vm.AvaliacaoGalpaoQualitativo}");

                if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    LogNavigation("OpenAvaliacaoGalpaoFormAsync aguardando estabilização iOS (350ms)");
                    await Task.Delay(350);
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var page = Microsoft.Extensions.DependencyInjection.ActivatorUtilities
                        .CreateInstance<SilvaData.Pages.LoteViews.AvaliacaoGalpaoFormView>(services);

                    var nav = Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;
                    if (nav == null)
                        throw new InvalidOperationException("Navigation indisponível para AvaliacaoGalpaoFormView.");

                    LogNavigation($"OpenAvaliacaoGalpaoFormAsync PushModalAsync | modalStack={nav.ModalStack.Count}");
                    await nav.PushModalAsync(page, false);
                    LogNavigation("OpenAvaliacaoGalpaoFormAsync PushModalAsync concluído");
                });

                return true;
            }
            catch (Exception ex)
            {
                LogNavigationError(nameof(OpenAvaliacaoGalpaoFormAsync), ex);
                await PopUpOK.ShowAsync("Erro", $"Falha ao abrir avaliação: {ex.Message}");
                return false;
            }
        }

        #endregion

        // New: public helper to show an already constructed Page as modal
        public static async Task ShowPageAsModalAsync(Page page, bool animated = true)
        {
            await ShowModalAsync(page, animated).ConfigureAwait(false);
        }
    }
}
