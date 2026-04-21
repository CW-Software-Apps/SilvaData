// Usings do MAUI e Community Toolkit
using System;
using System.Collections.Generic;
using System.Text;

namespace SilvaData.Models
{
    /// <summary>
    /// Classe que cont�m as informa��es do fechamento de lote.
    /// (Mantida como classe interna)
    /// </summary>
    public class LoginInfo
    {
        public string Email { get; set; }
        public string Senha { get; set; }
        public bool Sucesso { get; set; }
    }

}
