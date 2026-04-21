using Foundation;
using ObjCRuntime;
using UIKit;

namespace SilvaData
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Captura exce��es nativas ObjC antes de iniciar o app
            ObjCRuntime.Runtime.MarshalObjectiveCException += (sender, e) =>
            {
                Console.WriteLine($"[iOS-CRASH] ObjC Exception: {e.Exception.Name} � {e.Exception.Reason}");
                System.Diagnostics.Debug.WriteLine($"[iOS-CRASH] ObjC Exception: {e.Exception.Name} � {e.Exception.Reason}");
            };

            // Captura exce��es n�o tratadas do .NET
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Console.WriteLine($"[iOS-CRASH] Unhandled .NET Exception: {e.ExceptionObject}");
                System.Diagnostics.Debug.WriteLine($"[iOS-CRASH] Unhandled .NET Exception: {e.ExceptionObject}");
            };

            try
            {
                UIApplication.Main(args, null, typeof(AppDelegate));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[iOS-CRASH] Exception in Main: {ex}");
                System.Diagnostics.Debug.WriteLine($"[iOS-CRASH] Exception in Main: {ex}");
            }
        }
    }
}
