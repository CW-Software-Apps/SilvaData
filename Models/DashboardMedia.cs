using CommunityToolkit.Mvvm.ComponentModel; // Necess�rio para ObservableObject

namespace SilvaData.Models
{
    /// <summary>
    /// Modelo de dados (DTO) que armazena as m�dias
    /// exibidas nos cart�es do Dashboard (Home).
    /// </summary>
    public partial class DashboardMedia : ObservableObject
    {
        // Propriedades permanecem, mas podem ser ObservableProperties
        // se voc� precisar de binding granular.
        // Se voc� sempre substitui o objeto inteiro, { get; set; } � suficiente.
        public double mediaIsiMacroClienteUsuario { get; set; }
        public double mediaIsiMacroCliente { get; set; }
        public double mediaIsiMacroGlobal { get; set; }
        public double mediaScoreManejo { get; set; }
        public double mediaIEP { get; set; }
    }
}
