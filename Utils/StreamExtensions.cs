using System.Diagnostics;

namespace SilvaData.Utilities
{
    /// <summary>
    /// Extens�es para <see cref="HttpClient"/> que facilitam download com progresso.
    /// </summary>
    public static class HttpClientExtensions
    {
        private const int DefaultBufferSize = 81920; // .NET default

        /// <summary>
        /// Faz o download do conte�do de <paramref name="requestUri"/> para <paramref name="destination"/> com suporte a cancelamento e progresso.
        /// </summary>
        /// <param name="client">Inst�ncia de <see cref="HttpClient"/>.</param>
        /// <param name="requestUri">URL do recurso a ser baixado.</param>
        /// <param name="destination">Stream de destino (grav�vel) que receber� os bytes baixados.</param>
        /// <param name="progress">
        /// Progresso do download: valor fracion�rio entre 0.0 e 1.0 (0% a 100%).
        /// Reporta 0 no in�cio e 1 no final quando o tamanho do conte�do � conhecido.
        /// </param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns><c>true</c> em caso de sucesso; <c>false</c> caso ocorra alguma falha.</returns>
        public static async Task<bool> DownloadAsync(
            this HttpClient client,
            string requestUri,
            Stream destination,
            IProgress<float>? progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(client);
                if (string.IsNullOrWhiteSpace(requestUri)) throw new ArgumentException("requestUri inv�lido.", nameof(requestUri));
                ArgumentNullException.ThrowIfNull(destination);
                if (!destination.CanWrite) throw new ArgumentException("O stream de destino precisa ser grav�vel.", nameof(destination));

                using var response = await client
                    .GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                var contentLength = response.Content.Headers.ContentLength;

                await using var download = await response.Content
                    .ReadAsStreamAsync(cancellationToken)
                    .ConfigureAwait(false);

                // Sem progresso ou sem Content-Length: apenas copia
                if (progress == null || !contentLength.HasValue)
                {
                    await download.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
                    return true;
                }

                // Com progresso e Content-Length conhecido
                progress.Report(0f);
                var total = contentLength.Value;

                var relativeProgress = new Progress<long>(totalBytes =>
                {
                    var fraction = total <= 0 ? 0f : (float)totalBytes / total;
                    // Clampeia entre 0 e 1
                    if (fraction < 0f) fraction = 0f;
                    if (fraction > 1f) fraction = 1f;
                    progress.Report(fraction);
                });

                await download.CopyToAsync(destination, DefaultBufferSize, relativeProgress, cancellationToken).ConfigureAwait(false);

                // Garante 100% ao final
                progress.Report(1f);
                return true;
            }
            catch (OperationCanceledException)
            {
                // Cancelado pelo usu�rio/chamada
                Debug.WriteLine("DownloadAsync cancelado.");
                return false;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Falha no DownloadAsync: {e}");
                return false;
            }
        }
    }

    /// <summary>
    /// Extens�es para <see cref="Stream"/> com suporte a progresso e cancelamento.
    /// </summary>
    public static class StreamExtensions
    {
        private const int DefaultBufferSize = 81920; // .NET default

        /// <summary>
        /// Copia dados do <paramref name="source"/> para <paramref name="destination"/> com buffer e progresso.
        /// </summary>
        /// <param name="source">Stream de origem (leg�vel).</param>
        /// <param name="destination">Stream de destino (grav�vel).</param>
        /// <param name="bufferSize">Tamanho do buffer em bytes (&gt; 0).</param>
        /// <param name="progress">Progresso em bytes totais j� copiados.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        public static async Task CopyToAsync(
            this Stream source,
            Stream destination,
            int bufferSize,
            IProgress<long>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (!source.CanRead) throw new ArgumentException("O stream de origem precisa ser leg�vel.", nameof(source));
            if (destination is null) throw new ArgumentNullException(nameof(destination));
            if (!destination.CanWrite) throw new ArgumentException("O stream de destino precisa ser grav�vel.", nameof(destination));
            if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize), "O tamanho do buffer deve ser maior que zero.");

            var buffer = new byte[bufferSize];
            long totalBytesRead = 0;

            while (true)
            {
                var bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0) break;

                await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                totalBytesRead += bytesRead;
                progress?.Report(totalBytesRead);
            }

            // Garante flush do destino (quando aplic�vel)
            await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Copia dados do <paramref name="source"/> para <paramref name="destination"/> usando o buffer padr�o,
        /// com suporte a progresso e cancelamento.
        /// </summary>
        /// <param name="source">Stream de origem (leg�vel).</param>
        /// <param name="destination">Stream de destino (grav�vel).</param>
        /// <param name="progress">Progresso em bytes totais j� copiados.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        public static Task CopyToAsync(
            this Stream source,
            Stream destination,
            IProgress<long>? progress,
            CancellationToken cancellationToken = default)
            => CopyToAsync(source, destination, DefaultBufferSize, progress, cancellationToken);
    }
}
