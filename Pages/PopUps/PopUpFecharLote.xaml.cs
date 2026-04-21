using System;
using System.Threading.Tasks;

using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input; // MUDAN�A: Necess�rio para [RelayCommand]

using SilvaData.Models;

// Removido: using System.Windows.Input; (Substitu�do por CommunityToolkit.Mvvm.Input)

using SilvaData.Utilities;

namespace SilvaData.Pages.PopUps
{
    /// <summary>
    /// Popup para fechamento de lote com campos para data e observa��es.
    /// </summary>
    // MUDAN�A: A classe aninhada n�o precisa do 'static using' na pr�pria classe
    public partial class PopUpFecharLote : Popup<LoteFechamentoInfo>
    {
        /// <summary>
        /// Inicializa uma nova inst�ncia do popup de fechamento de lote.
        /// </summary>
        /// <param name="titulo">T�tulo do popup</param>
        /// <param name="mensagem">Mensagem explicativa</param>
        public PopUpFecharLote(string titulo, string mensagem)
        {
            InitializeComponent();
            BindingContext = new PopUpFecharLoteViewModel(this, titulo, mensagem);
        }

        /// <summary>
        /// Exibe um popup para fechamento de lote com campos para data e observa��es.
        /// </summary>
        /// <param name="titulo">T�tulo do popup</param>
        /// <param name="mensagem">Mensagem explicativa</param>
        /// <returns>Informa��es do fechamento do lote (data, observa��es, confirmado)</returns>
        public static async Task<LoteFechamentoInfo> ShowAsync(string titulo, string mensagem)
        {
            if (string.IsNullOrEmpty(titulo))
                titulo = "Fechar Lote";

            var popup = new PopUpFecharLote(titulo, mensagem);

            // Usa o m�todo gen�rico do NavigationUtils (migrado)
            var result = await NavigationUtils.ShowPopupAsync<LoteFechamentoInfo>(popup);

            // Garante que nunca retorne nulo, fornecendo valores padr�o
            return result ?? LoteFechamentoInfo.Default();
        }
    }

    /// <summary>
    /// ViewModel para o popup de fechamento de lote.
    /// </summary>
    // MUDAN�A: A classe precisa ser 'partial' para o MVVM Toolkit
    public partial class PopUpFecharLoteViewModel : ObservableObject
    {
        // MUDAN�A: A refer�ncia ao PopUp usa a classe PopUpFecharLote
        private readonly PopUpFecharLote _popup;

        /// <summary>
        /// T�tulo do popup.
        /// </summary>
        public string Titulo { get; }

        /// <summary>
        /// Mensagem explicativa.
        /// </summary>
        public string Mensagem { get; }

        /// <summary>
        /// Data de fechamento do lote.
        /// </summary>
        [ObservableProperty]
        private DateTime dataFechamento = DateTime.Now;

        /// <summary>
        /// Observa��es sobre o fechamento.
        /// </summary>
        [ObservableProperty]
        private string observacoes = string.Empty;

        // MUDAN�A: Comandos manuais ICommand removidos.

        /// <summary>
        /// Inicializa uma nova inst�ncia do ViewModel.
        /// </summary>
        public PopUpFecharLoteViewModel(PopUpFecharLote popup, string titulo, string mensagem)
        {
            _popup = popup ?? throw new ArgumentNullException(nameof(popup));
            Titulo = titulo ?? "Fechar Lote";
            Mensagem = mensagem ?? string.Empty;

            // MUDAN�A: Comandos n�o s�o mais inicializados manualmente
        }

        // MUDAN�A: Usando [RelayCommand] para o comando de Confirma��o
        [RelayCommand]
        private Task ConfirmarAsync() // MUDAN�A: M�todo ass�ncrono que retorna Task
        {
            // Cria um objeto com as informa��es do fechamento
            var info = new LoteFechamentoInfo
            {
                DataFechamento = DataFechamento,
                Observacoes = Observacoes ?? string.Empty,
                Confirmado = true
            };

            // MUDAN�A: Fecha o popup usando CloseAsync (sempre a forma preferida)
            return _popup.CloseAsync(info);
        }

        // MUDAN�A: Usando [RelayCommand] para o comando de Cancelamento
        [RelayCommand]
        private Task CancelarAsync() // MUDAN�A: M�todo ass�ncrono que retorna Task
        {
            // Cria um objeto com as informa��es, mas com Confirmado = false
            var info = new LoteFechamentoInfo
            {
                // Mantemos a data e observa��es para consist�ncia de dados
                DataFechamento = DataFechamento,
                Observacoes = Observacoes ?? string.Empty,
                Confirmado = false
            };

            // MUDAN�A: Fecha o popup usando CloseAsync (sempre a forma preferida)
            return _popup.CloseAsync(info);
        }
    }
}
