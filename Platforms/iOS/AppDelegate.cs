using Foundation;
using UIKit;

namespace SilvaData
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp()
        {
            System.Console.WriteLine("[AppDelegate] CreateMauiApp INICIANDO...");
            System.Diagnostics.Debug.WriteLine("[AppDelegate] CreateMauiApp INICIANDO...");

            try
            {
                var app = MauiProgram.CreateMauiApp();
                System.Console.WriteLine("[AppDelegate] CreateMauiApp CONCLU�DO com sucesso.");
                System.Diagnostics.Debug.WriteLine("[AppDelegate] CreateMauiApp CONCLU�DO com sucesso.");
                return app;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[AppDelegate] EXCE��O em CreateMauiApp: {ex}");
                System.Diagnostics.Debug.WriteLine($"[AppDelegate] EXCE��O em CreateMauiApp: {ex}");
                throw;
            }
        }

        public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
        {
            System.Console.WriteLine("[AppDelegate] FinishedLaunching INICIANDO...");
            System.Diagnostics.Debug.WriteLine("[AppDelegate] FinishedLaunching INICIANDO...");

            try
            {
                var result = base.FinishedLaunching(application, launchOptions);
                System.Console.WriteLine("[AppDelegate] FinishedLaunching CONCLU�DO com sucesso.");
                System.Diagnostics.Debug.WriteLine("[AppDelegate] FinishedLaunching CONCLU�DO com sucesso.");
                return result;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[AppDelegate] EXCE��O em FinishedLaunching: {ex}");
                System.Diagnostics.Debug.WriteLine($"[AppDelegate] EXCE��O em FinishedLaunching: {ex}");
                throw;
            }
        }
    }
}
