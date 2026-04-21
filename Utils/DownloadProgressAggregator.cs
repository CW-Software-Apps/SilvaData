using System;
using System.Threading;

namespace SilvaData.Utilities
{
    /// <summary>
    /// Agrega m�ltiplos downloads e reporta o progresso consolidado
    /// </summary>
    public class DownloadProgressAggregator
    {
        private readonly int _totalItems;
        private readonly Action<int, int, float> _progressCallback;
        private int _completedItems;
        private readonly object _lockObject = new object();

        /// <summary>
        /// Cria um novo agregador de progresso de download
        /// </summary>
        /// <param name="totalItems">Total de itens a serem baixados</param>
        /// <param name="progressCallback">Callback (completados, total, percentual)</param>
        public DownloadProgressAggregator(int totalItems, Action<int, int, float> progressCallback)
        {
            _totalItems = totalItems;
            _progressCallback = progressCallback;
            _completedItems = 0;
        }

        /// <summary>
        /// Cria um IProgress para um item individual
        /// </summary>
        public IProgress<float> CreateProgress()
        {
            return new Progress<float>(percent =>
            {
                if (percent >= 100) // Item completo
                {
                    lock (_lockObject)
                    {
                        _completedItems++;
                        var overallPercent = (_completedItems / (float)_totalItems) * 100f;
                        _progressCallback?.Invoke(_completedItems, _totalItems, overallPercent);
                    }
                }
            });
        }

        /// <summary>
        /// Marca manualmente um item como completo (para downloads sem progresso detalhado)
        /// </summary>
        public void ReportItemCompleted()
        {
            lock (_lockObject)
            {
                _completedItems++;
                var overallPercent = (_completedItems / (float)_totalItems) * 100f;
                _progressCallback?.Invoke(_completedItems, _totalItems, overallPercent);
            }
        }

        /// <summary>
        /// Obt�m o n�mero de itens completados
        /// </summary>
        public int CompletedItems
        {
            get
            {
                lock (_lockObject)
                {
                    return _completedItems;
                }
            }
        }

        /// <summary>
        /// Obt�m o percentual de conclus�o
        /// </summary>
        public float PercentComplete
        {
            get
            {
                lock (_lockObject)
                {
                    return (_completedItems / (float)_totalItems) * 100f;
                }
            }
        }
    }
}
