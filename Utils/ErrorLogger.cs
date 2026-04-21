using SilvaData.Models;
using System.Diagnostics;
using System.Text.Json;

namespace SilvaData.Utils
{
    /// <summary>
    /// Logger unificado para erros do WebService com múltiplos destinos
    /// </summary>
    public static class ErrorLogger
    {
        private static readonly string LogFilePath = Path.Combine(
            FileSystem.CacheDirectory, 
            "error_logs", 
            $"errors_{DateTime.Now:yyyyMMdd}.json"
        );

        private static readonly object _fileLock = new object();
        private static readonly List<ErrorDetails> _memoryCache = new();
        private const int MaxMemoryCache = 100; // Mantém últimos 100 erros em memória

        static ErrorLogger()
        {
            // Garante que o diretório de logs exista
            var logDir = Path.GetDirectoryName(LogFilePath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
        }

        /// <summary>
        /// Registra um erro com detalhes completos
        /// </summary>
        public static async Task LogErrorAsync(ErrorDetails errorDetails)
        {
            if (errorDetails == null) return;

            try
            {
                // 1. Log no Debug (development)
                LogToDebug(errorDetails);

                // 2. Log em arquivo local
                await LogToFileAsync(errorDetails);

                // 3. Log no Sentry (se não for erro esperado) - DESABILITADO
                // if (!errorDetails.ShouldIgnoreInSentry)
                // {
                //     LogToSentry(errorDetails);
                // }

                // 4. Cache em memória para UI
                AddToMemoryCache(errorDetails);

                Debug.WriteLine($"[ErrorLogger] Erro registrado: {errorDetails.ToSummary()}");
            }
            catch (Exception ex)
            {
                // Failsafe - se o logger falhar, pelo menos log no Debug
                Debug.WriteLine($"[ErrorLogger] FALHA AO LOGAR ERRO: {ex.Message}");
                Debug.WriteLine($"[ErrorLogger] Erro original: {errorDetails?.ToSummary()}");
            }
        }

        /// <summary>
        /// Cria ErrorDetails a partir de uma exceção e contexto
        /// </summary>
        public static ErrorDetails CreateFromException(
            Exception exception,
            string methodName,
            string requestId,
            string requestUrl = "",
            string requestPayload = "",
            string requestPayloadDecrypted = "",
            int? statusCode = null,
            string responseContent = "",
            TimeSpan? responseTime = null,
            int retryCount = 0)
        {
            var errorType = ClassifyError(exception, statusCode, responseContent);
            var userFriendlyMessage = GetUserFriendlyMessage(errorType, exception);

            // Preenche informações do usuário se disponível
            string? userId = null;
            string? userName = null;
            string? deviceId = null;

            try
            {
                var webService = ISIWebService.Instance;
                if (webService.LoggedUser != null)
                {
                    userId = webService.LoggedUser.id;
                    userName = webService.LoggedUser.nome;
                    deviceId = webService.LoggedUser.dispositivoId;
                }
            }
            catch
            {
                // Se não conseguir obter informações do usuário, continua sem elas
            }

            return new ErrorDetails
            {
                RequestId = requestId,
                Timestamp = DateTime.UtcNow,
                ErrorType = errorType,
                MethodName = methodName,
                ExceptionMessage = exception.Message,
                ExceptionType = exception.GetType().Name,
                StackTrace = exception.StackTrace ?? "",
                UserFriendlyMessage = userFriendlyMessage,
                RequestUrl = requestUrl,
                RequestPayload = requestPayload,
                RequestPayloadDecrypted = requestPayloadDecrypted,
                StatusCode = statusCode,
                ResponseContent = responseContent,
                ResponseTime = responseTime,
                UserId = userId,
                UserName = userName,
                DeviceId = deviceId,
                RetryCount = retryCount,
                IsRetryable = IsRetryableError(errorType),
                ShouldIgnoreInSentry = ShouldIgnoreInSentry(errorType, exception),
                Context = new Dictionary<string, object>
                {
                    ["InnerException"] = exception.InnerException?.Message ?? "",
                    ["Source"] = exception.Source ?? ""
                }
            };
        }

        /// <summary>
        /// Classifica o tipo de erro baseado na exceção e contexto
        /// </summary>
        private static ErrorType ClassifyError(Exception exception, int? statusCode, string responseContent)
        {
            // Erros de rede/timeouts
            if (exception is System.Net.Sockets.SocketException ||
                exception is System.IO.IOException ||
                exception is HttpRequestException ||
                exception is TimeoutException ||
                exception is OperationCanceledException)
            {
                return exception is TimeoutException || exception is OperationCanceledException
                    ? ErrorType.TimeoutError
                    : ErrorType.NetworkError;
            }

            // Erros HTTP específicos
            if (statusCode.HasValue)
            {
                return statusCode.Value switch
                {
                    >= 500 => ErrorType.ServerError,
                    401 or 403 => ErrorType.AuthenticationError,
                    400 => ErrorType.BusinessLogicError,
                    _ => ErrorType.Unknown
                };
            }

            // Erros de HTML do servidor
            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                var trimmed = responseContent.TrimStart();
                if (trimmed.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
                {
                    return ErrorType.HtmlError;
                }
            }

            // Erros de serialização/deserialização
            if (exception is JsonException || 
                exception.Message.Contains("JSON", StringComparison.OrdinalIgnoreCase) ||
                exception.Message.Contains("deserialize", StringComparison.OrdinalIgnoreCase))
            {
                return ErrorType.SerializationError;
            }

            // Erros de descriptografia
            if (exception.Message.Contains("decrypt", StringComparison.OrdinalIgnoreCase) ||
                exception.Message.Contains("crypt", StringComparison.OrdinalIgnoreCase))
            {
                return ErrorType.DecryptionError;
            }

            return ErrorType.Unknown;
        }

        /// <summary>
        /// Retorna mensagem amigável para o usuário
        /// </summary>
        private static string GetUserFriendlyMessage(ErrorType errorType, Exception exception)
        {
            return errorType switch
            {
                ErrorType.NetworkError => "A conexão com o servidor foi interrompida ou está instável.",
                ErrorType.TimeoutError => "Tempo limite excedido ao tentar comunicar com o servidor.",
                ErrorType.ServerError => "O servidor encontrou um erro interno. Tente novamente em alguns minutos.",
                ErrorType.AuthenticationError => "Sua sessão expirou. Faça login novamente.",
                ErrorType.SerializationError => "Erro ao processar dados da requisição.",
                ErrorType.DecryptionError => "Erro ao processar resposta do servidor.",
                ErrorType.HtmlError => "O servidor retornou uma página de erro em vez de dados.",
                ErrorType.CircuitBreakerOpen => "Servidor temporariamente indisponível. Aguardando recuperação.",
                _ => $"Ocorreu um erro: {exception.Message}"
            };
        }

        /// <summary>
        /// Determina se o erro pode ser retentado
        /// </summary>
        private static bool IsRetryableError(ErrorType errorType)
        {
            return errorType switch
            {
                ErrorType.NetworkError => true,
                ErrorType.TimeoutError => true,
                ErrorType.ServerError => true,
                ErrorType.HtmlError => true,
                _ => false
            };
        }

        /// <summary>
        /// Determina se o erro deve ser ignorado no Sentry
        /// </summary>
        private static bool ShouldIgnoreInSentry(ErrorType errorType, Exception exception)
        {
            // Erros de rede esperados não devem poluir o Sentry
            if (errorType == ErrorType.NetworkError || errorType == ErrorType.TimeoutError)
                return true;

            // Verifica mensagens específicas de rede
            var msg = exception.Message.ToLowerInvariant();
            if (msg.Contains("socket closed") ||
                msg.Contains("connection abort") ||
                msg.Contains("connection reset") ||
                msg.Contains("network is unreachable") ||
                msg.Contains("the network connection was lost") ||
                msg.Contains("connection failure") ||
                msg.Contains("no address associated with hostname") ||
                msg.Contains("unable to resolve host"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Log no Debug para development
        /// </summary>
        private static void LogToDebug(ErrorDetails errorDetails)
        {
            Debug.WriteLine($"[ERROR] {errorDetails.ToSummary()}");
            
            if (Debugger.IsAttached)
            {
                Debug.WriteLine(errorDetails.ToFullDetails());
            }
        }

        /// <summary>
        /// Log em arquivo local
        /// </summary>
        private static async Task LogToFileAsync(ErrorDetails errorDetails)
        {
            try
            {
                var logEntry = new
                {
                    timestamp = errorDetails.Timestamp,
                    request_id = errorDetails.RequestId,
                    error_type = errorDetails.ErrorType.ToString(),
                    method = errorDetails.MethodName,
                    exception_type = errorDetails.ExceptionType,
                    message = errorDetails.ExceptionMessage,
                    user_message = errorDetails.UserFriendlyMessage,
                    status_code = errorDetails.StatusCode,
                    retry_count = errorDetails.RetryCount,
                    is_retryable = errorDetails.IsRetryable,
                    user_id = errorDetails.UserId,
                    device_id = errorDetails.DeviceId,
                    context = errorDetails.Context
                };

                var json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions 
                { 
                    WriteIndented = false 
                });

                lock (_fileLock)
                {
                    File.AppendAllText(LogFilePath, json + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ErrorLogger] Falha ao escrever em arquivo: {ex.Message}");
            }
        }

        /// <summary>
        /// Adiciona ao cache em memória
        /// </summary>
        private static void AddToMemoryCache(ErrorDetails errorDetails)
        {
            lock (_memoryCache)
            {
                _memoryCache.Add(errorDetails);
                
                // Mantém apenas os últimos N erros
                if (_memoryCache.Count > MaxMemoryCache)
                {
                    _memoryCache.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// Obtém erros recentes do cache em memória
        /// </summary>
        public static List<ErrorDetails> GetRecentErrors(int count = 50)
        {
            lock (_memoryCache)
            {
                return _memoryCache.TakeLast(count).ToList();
            }
        }

        /// <summary>
        /// Limpa logs antigos (mantém apenas últimos 7 dias)
        /// </summary>
        public static void CleanupOldLogs()
        {
            try
            {
                var logDir = Path.GetDirectoryName(LogFilePath);
                if (string.IsNullOrEmpty(logDir) || !Directory.Exists(logDir))
                    return;

                var files = Directory.GetFiles(logDir, "errors_*.json");
                var cutoffDate = DateTime.Now.AddDays(-7);

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        try
                        {
                            File.Delete(file);
                            Debug.WriteLine($"[ErrorLogger] Log antigo removido: {file}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[ErrorLogger] Falha ao remover log antigo {file}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ErrorLogger] Falha na limpeza de logs: {ex.Message}");
            }
        }

        /// <summary>
        /// Exporta todos os logs para um arquivo
        /// </summary>
        public static async Task<string> ExportAllLogsAsync()
        {
            try
            {
                var logDir = Path.GetDirectoryName(LogFilePath);
                if (string.IsNullOrEmpty(logDir) || !Directory.Exists(logDir))
                    return "Nenhum log encontrado.";

                var files = Directory.GetFiles(logDir, "errors_*.json")
                    .OrderByDescending(f => f)
                    .ToList();

                var allLogs = new List<string>();
                
                foreach (var file in files)
                {
                    var content = await File.ReadAllTextAsync(file);
                    allLogs.Add($"=== ARQUIVO: {Path.GetFileName(file)} ===");
                    allLogs.Add(content);
                    allLogs.Add("");
                }

                var exportPath = Path.Combine(FileSystem.CacheDirectory, $"error_export_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                await File.WriteAllTextAsync(exportPath, string.Join(Environment.NewLine, allLogs));
                
                return exportPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ErrorLogger] Falha ao exportar logs: {ex.Message}");
                return $"Falha ao exportar: {ex.Message}";
            }
        }
    }
}
