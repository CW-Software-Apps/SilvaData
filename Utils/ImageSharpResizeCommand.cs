using SkiaSharp;

using System.Diagnostics;

namespace SilvaData.Utilities
{
    // --- Interfaces e Classes de Contexto ---

    public interface IResizeImageCommand
    {
        Task<ResizeImageContext> ExecuteAsync(ResizeImageContext context);
    }

    public class ResizeImageContext
    {
        public byte[]? OriginalImage { get; set; }
        public byte[]? ResizedImage { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public float Quality { get; set; } = 0.75f; // Qualidade JPEG padr�o (75%)
        public TaskResult TaskResult { get; set; }
    }

    public enum TaskResult { Success, Canceled, Faulted }

    /// <summary>
    /// Implementa��o de redimensionamento de imagem usando SkiaSharp.
    /// Oferece melhor performance e menor uso de mem�ria no MAUI Android.
    /// </summary>
    public class SkiaSharpResizeCommand : IResizeImageCommand
    {
        public async Task<ResizeImageContext> ExecuteAsync(ResizeImageContext context)
        {
            if (context.OriginalImage is null || context.OriginalImage.Length == 0)
            {
                Debug.WriteLine("[SkiaSharpResize] Imagem original nula ou vazia");
                context.TaskResult = TaskResult.Faulted;
                return context;
            }

            try
            {
                // Executar processamento em thread separada para n�o bloquear UI
                await Task.Run(() => ProcessResize(context)).ConfigureAwait(false);
                return context;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SkiaSharpResize] Erro cr�tico: {ex.Message}");
                context.TaskResult = TaskResult.Faulted;
                return context;
            }
        }

        private void ProcessResize(ResizeImageContext context)
        {
            if (context.OriginalImage is null) return;

            SKBitmap? original = null;
            SKBitmap? resized = null;
            SKImage? image = null;
            SKData? encoded = null;

            try
            {
                // 1. Decodificar imagem original
                original = SKBitmap.Decode(context.OriginalImage);

                if (original == null)
                {
                    Debug.WriteLine("[SkiaSharpResize] Falha ao decodificar imagem");
                    context.TaskResult = TaskResult.Faulted;
                    return;
                }

                if (original.Width <= 0 || original.Height <= 0)
                {
                    Debug.WriteLine("[SkiaSharpResize] Dimens�es inv�lidas");
                    context.TaskResult = TaskResult.Faulted;
                    return;
                }

                Debug.WriteLine($"[SkiaSharpResize] Imagem original: {original.Width}x{original.Height}, {context.OriginalImage.Length:N0} bytes");

                // 2. Calcular escala mantendo propor��o
                float scale = Math.Min(
                    (float)context.Width / original.Width,
                    (float)context.Height / original.Height
                );

                // Se a imagem j� � menor que o tamanho desejado, retornar original
                if (scale >= 1.0f)
                {
                    Debug.WriteLine($"[SkiaSharpResize] Imagem original mantida (escala {scale:F2})");
                    context.ResizedImage = context.OriginalImage;
                    context.TaskResult = TaskResult.Success;
                    return;
                }

                // 3. Calcular novas dimens�es
                int newWidth = (int)(original.Width * scale);
                int newHeight = (int)(original.Height * scale);

                Debug.WriteLine($"[SkiaSharpResize] Redimensionando para {newWidth}x{newHeight} (escala {scale:F2})");

                // 4. Redimensionar com filtro de alta qualidade
                var resizeInfo = new SKImageInfo(newWidth, newHeight);
                resized = original.Resize(resizeInfo, new SKSamplingOptions(SKCubicResampler.Mitchell));

                if (resized == null)
                {
                    Debug.WriteLine("[SkiaSharpResize] Falha ao redimensionar bitmap");
                    context.TaskResult = TaskResult.Faulted;
                    return;
                }

                // 5. Converter para SKImage e codificar como JPEG
                image = SKImage.FromBitmap(resized);

                if (image == null)
                {
                    Debug.WriteLine("[SkiaSharpResize] Falha ao criar SKImage");
                    context.TaskResult = TaskResult.Faulted;
                    return;
                }

                // Codificar com qualidade especificada (0-100)
                int quality = (int)(context.Quality * 100);
                encoded = image.Encode(SKEncodedImageFormat.Jpeg, quality);

                if (encoded == null)
                {
                    Debug.WriteLine("[SkiaSharpResize] Falha ao codificar imagem");
                    context.TaskResult = TaskResult.Faulted;
                    return;
                }

                // 6. Converter para byte array
                context.ResizedImage = encoded.ToArray();
                context.TaskResult = TaskResult.Success;

                // 7. Log de sucesso
                float originalLength = context.OriginalImage.Length;
                float resizedLength = context.ResizedImage.Length;
                float reducao = (1 - (resizedLength / originalLength)) * 100;

                Debug.WriteLine($"[SkiaSharpResize] ? Sucesso: {originalLength:N0} bytes -> {resizedLength:N0} bytes (redu��o de {reducao:F1}%)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SkiaSharpResize] Erro no processamento: {ex.Message}");
                Debug.WriteLine($"[SkiaSharpResize] StackTrace: {ex.StackTrace}");
                context.TaskResult = TaskResult.Faulted;
            }
            finally
            {
                // IMPORTANTE: Liberar TODA mem�ria nativa
                // SkiaSharp usa mem�ria n�o-gerenciada que precisa ser liberada manualmente
                encoded?.Dispose();
                image?.Dispose();
                resized?.Dispose();
                original?.Dispose();

                Debug.WriteLine("[SkiaSharpResize] Recursos liberados");
            }
        }
    }
}
