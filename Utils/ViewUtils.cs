using SilvaData.Models;
using SilvaData.Utils;

using System.IO.Compression;

namespace ISIInstitute.Views
{
    public class ViewUtils
    {
        public static async Task<string> FazbackupCompleto()
        {
            await Database.CloseDatabaseAsync();

            //Salva Informa��o de Login
            var loginInfo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "logininfo.txt");
            var user = Preferences.Get("user", null);
            await File.WriteAllTextAsync(loginInfo, user);

            var deviceid = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "deviceid.txt");
            var device = Preferences.Get("my_id", null);
            await File.WriteAllTextAsync(deviceid, device);

            var zipparaenviar = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), $"ISIApp {DateTime.Now:dd-MM-yyyy-HH_mm} - {ISIWebService.Instance.LoggedUser.nome.Replace("/", "-")}.zip");

            var arquivos = new List<string> { Database.PathDB, loginInfo, deviceid };

            arquivos.AddRange(await LoteFormImagem.ListaImagensParaBackup());

            QuickZip(arquivos.ToArray(), zipparaenviar);

            await Database.ReopenDatabaseAsync();

            return zipparaenviar;
        }


        public static bool QuickZip(string[] filesToZip, string destinationZipFullPath)
        {
            try
            {
                // Delete existing zip file if exists
                if (File.Exists(destinationZipFullPath)) File.Delete(destinationZipFullPath);

                using (ZipArchive zip = ZipFile.Open(destinationZipFullPath, ZipArchiveMode.Create))
                {
                    foreach (var file in filesToZip)
                        zip.CreateEntryFromFile(file, Path.GetFileName(file), CompressionLevel.Optimal);
                }

                return File.Exists(destinationZipFullPath);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: {e.Message}");
                return false;
            }
        }


    }
}
