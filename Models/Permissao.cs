using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Reflection;
using Newtonsoft.Json;
using System.Diagnostics;

namespace SilvaData.Models
{
    /// <summary>
    /// Classe base para permissões com operações CRUD (Consultar, Cadastrar, Atualizar, Excluir).
    /// </summary>
    public abstract class PermissaoCrudBase
    {
        public bool consultar { get; set; }
        public bool cadastrar { get; set; }
        public bool atualizar { get; set; }
        public bool excluir { get; set; }
    }

    /// <summary>
    /// Classe base para permissões com operações de Consulta e Edição.
    /// </summary>
    public abstract class PermissaoConsultaEdicaoBase
    {
        public bool consultar { get; set; }
        public bool editar { get; set; }
    }

    public class RegionaisPermissoes : PermissaoCrudBase { }
    public class Propriedades : PermissaoCrudBase { }
    public class ProprietariosPermissoes : PermissaoCrudBase { }
    public class UnidadesEpidemiologicasPermissoes : PermissaoCrudBase { }
    public class AtividadesPermissoes : PermissaoCrudBase { }

    public class ManejoPermissoes : PermissaoConsultaEdicaoBase { }
    public class SanidadePermissoes : PermissaoConsultaEdicaoBase { }
    public class ZootecnicoPermissoes : PermissaoConsultaEdicaoBase { }
    public class IsiMacroPermissoes : PermissaoConsultaEdicaoBase { }
    public class NutricaoPermissoes : PermissaoConsultaEdicaoBase { }
    public class IsiMicroPermissoes : PermissaoConsultaEdicaoBase { }
    public class GalpaoPermissoes : PermissaoConsultaEdicaoBase { }

    public class Lotes
    {
        public bool consultar { get; set; }
        public bool cadastrar { get; set; }
        public bool abrir { get; set; }
        public Monitoramento monitoramento { get; set; } = new();
        public bool fechar { get; set; }
        public bool excluirForm { get; set; }
        public bool atualizar { get; set; }
        public bool excluir { get; set; }
    }

    public class Monitoramento
    {
        public SanidadePermissoes sanidade { get; set; } = new();
        public ZootecnicoPermissoes zootecnico { get; set; } = new();
        public IsiMacroPermissoes isiMacro { get; set; } = new();
        public NutricaoPermissoes nutricao { get; set; } = new();
        public IsiMicroPermissoes isiMicro { get; set; } = new();
        public ManejoPermissoes manejo { get; set; } = new();
        public GalpaoPermissoes galpao { get; set; } = new();
    }

    public class SistemaTermos
    {
        public string DESCRICAO { get; set; }
        public int ID { get; set; } = 1;
    }

    /// <summary>
    /// Gerencia centralizadamente as permissões do usuário na aplicação.
    /// </summary>
    public class Permissoes
    {
        public SistemaTermos SistemaTermos { get; set; } = new();
        public RegionaisPermissoes regionais { get; set; } = new();
        public UnidadesEpidemiologicasPermissoes unidadesEpidemiologicas { get; set; } = new();
        public ProprietariosPermissoes proprietarios { get; set; } = new();
        public AtividadesPermissoes atividades { get; set; } = new();
        public Lotes lotes { get; set; } = new();
        public Propriedades propriedades { get; set; } = new();
        
        [JsonProperty("loteDetalhado")]
        public bool LoteDetalhado { get; set; } = true;

        /// <summary>
        /// Instância estática única contendo as permissões do usuário logado.
        /// </summary>
        public static Permissoes UsuarioPermissoes { get; set; } = new();

        public static event PropertyChangedEventHandler StaticPropertyChanged;

        /// <summary>
        /// Dispara o evento StaticPropertyChanged para notificar a UI sobre mudanças.
        /// </summary>
        /// <param name="propertyName">Nome da propriedade estática que mudou.</param>
        private static void NotifyStaticPropertyChanged([CallerMemberName] string propertyName = "")
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }

        #region Propriedades Estáticas Calculadas (Simplificadas)

        public static bool PermissaoLoteSimplificado => !UsuarioPermissoes.LoteDetalhado;
        public static bool PermissaoLoteDetalhado => UsuarioPermissoes.LoteDetalhado;
        public static bool PodeVerManejo => UsuarioPermissoes.lotes.monitoramento.manejo.consultar;
        public static bool PodeVerZootecnico => UsuarioPermissoes.lotes.monitoramento.zootecnico.consultar;
        public static bool PodeVerIsiMacro => UsuarioPermissoes.lotes.monitoramento.isiMacro.consultar;
        public static bool PodeVerNutricao => UsuarioPermissoes.lotes.monitoramento.nutricao.consultar;
        public static bool PodeVerSanidade => UsuarioPermissoes.lotes.monitoramento.sanidade.consultar;
        public static bool PodeVerIsiMicro => UsuarioPermissoes.lotes.monitoramento.isiMicro.consultar;
        public static bool PodeVerAvaliacaoGalpao => UsuarioPermissoes.lotes.monitoramento.galpao.consultar; // Corrigido para usar galpao.consultar
        public static bool PodeVerGalpao => UsuarioPermissoes.lotes.monitoramento.galpao.consultar; // Nova propriedade para consistência
        public static bool PodeEditarZootecnico => UsuarioPermissoes.lotes.monitoramento.zootecnico.editar;
        public static bool TratamentoEmVezDeLote => UsuarioPermissoes.SistemaTermos.ID == 2;

        public static bool PodeAdicionarUE => UsuarioPermissoes.unidadesEpidemiologicas.cadastrar;
        public static bool PodeEditarUE => UsuarioPermissoes.unidadesEpidemiologicas.atualizar;

        // Compatibilidade com XAML antigo
        public static bool PodeAdicionar => PodeAdicionarUE;
        public static bool PodeEditar => PodeEditarUE;

        #endregion

        /// <summary>
        /// Notifica todas as propriedades estáticas calculadas sobre mudanças.
        /// Usado principalmente pelo popup de permissões em DEBUG mode.
        /// </summary>
        public static void NotifyAllStaticPropertiesChanged()
        {
            NotifyStaticPropertyChanged(nameof(PermissaoLoteSimplificado));
            NotifyStaticPropertyChanged(nameof(PermissaoLoteDetalhado));
            NotifyStaticPropertyChanged(nameof(PodeVerManejo));
            NotifyStaticPropertyChanged(nameof(PodeVerZootecnico));
            NotifyStaticPropertyChanged(nameof(PodeVerIsiMacro));
            NotifyStaticPropertyChanged(nameof(PodeVerNutricao));
            NotifyStaticPropertyChanged(nameof(PodeVerSanidade));
            NotifyStaticPropertyChanged(nameof(PodeVerIsiMicro));
            NotifyStaticPropertyChanged(nameof(PodeVerAvaliacaoGalpao));
            NotifyStaticPropertyChanged(nameof(PodeVerGalpao));
            NotifyStaticPropertyChanged(nameof(PodeEditarZootecnico));
            NotifyStaticPropertyChanged(nameof(TratamentoEmVezDeLote));
        }

        /// <summary>
        /// Carrega as permissões salvas nas Preferências do dispositivo.
        /// </summary>
        public static void ChecaPermissoes()
        {
            var permissoessalvas = Preferences.Get("Permissoes", "");

            try
            {
                if (!string.IsNullOrEmpty(permissoessalvas))
                {
                    UsuarioPermissoes = JsonConvert.DeserializeObject<Permissoes>(permissoessalvas);
                    Debug.WriteLine("[Permissoes] Permissões carregadas do Preferences com sucesso.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Permissoes] Erro ao desserializar permissões: {ex.Message}");
            }
            finally
            {
                // ADICIONADO: Validação null-safe
                if (UsuarioPermissoes == null)
                {
                    UsuarioPermissoes = new Permissoes();
                    Debug.WriteLine("[Permissoes] Permissões inicializadas com valores padrão.");
                }

                // ADICIONADO: Notifica todas as propriedades
                NotifyStaticPropertyChanged(nameof(PermissaoLoteSimplificado));
                NotifyStaticPropertyChanged(nameof(PermissaoLoteDetalhado));
                NotifyStaticPropertyChanged(nameof(PodeVerManejo));
                NotifyStaticPropertyChanged(nameof(PodeVerZootecnico));
                NotifyStaticPropertyChanged(nameof(PodeVerIsiMacro));
                NotifyStaticPropertyChanged(nameof(PodeVerNutricao));
                NotifyStaticPropertyChanged(nameof(PodeVerSanidade));
                NotifyStaticPropertyChanged(nameof(PodeVerIsiMicro));
                NotifyStaticPropertyChanged(nameof(PodeVerAvaliacaoGalpao));
                NotifyStaticPropertyChanged(nameof(PodeVerGalpao));
                NotifyStaticPropertyChanged(nameof(PodeEditarZootecnico));
                NotifyStaticPropertyChanged(nameof(TratamentoEmVezDeLote));
            }
        }

        /// <summary>
        /// Método auxiliar que usa Reflexão para definir recursivamente todas as
        /// propriedades 'bool' de um objeto e seus filhos como 'true'.
        /// Usado apenas para o modo DEBUG.
        /// </summary>
        /// <param name="obj">O objeto raiz para começar (ex: UsuarioPermissoes)</param>
        private static void SetAllBooleansToTrue(object obj)
        {
            if (obj == null) return;

            var type = obj.GetType();

            // ADICIONADO: Validação de namespace
            if (!type.Namespace?.StartsWith("SilvaData.Models") ?? false)
                return;

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                if (!prop.CanWrite) continue;

                try
                {
                    if (prop.PropertyType == typeof(bool))
                    {
                        prop.SetValue(obj, true);
                    }
                    else if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                    {
                        // ADICIONADO: Recursivamente
                        SetAllBooleansToTrue(prop.GetValue(obj));
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SetAllBooleansToTrue] Erro ao processar {prop.Name}: {ex.Message}");
                }
            }
        }
    }
}
