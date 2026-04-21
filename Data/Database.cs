using SQLite;

namespace SilvaData.Models
{
    /// <summary>
    /// Gerencia a conex�o singleton ass�ncrona com o banco de dados SQLite.
    /// </summary>
    public class Database
    {
        private static Database? _database;

        // Lock s�ncrono para a cria��o da inst�ncia
        private static readonly object _lockObject = new object();

        // Lock ass�ncrono para garantir que a inicializa��o ocorra apenas uma vez
        private static readonly SemaphoreSlim _asyncLock = new SemaphoreSlim(1, 1);

        private static bool _isInitialized = false;

        /// <summary>
        /// Conex�o ass�ncrona com o banco de dados.
        /// </summary>
        public SQLiteAsyncConnection sqlConnection { get; private set; }

        /// <summary>
        /// Obt�m o caminho completo para o arquivo de banco de dados no armazenamento local do aplicativo.
        /// </summary>
        public static string PathDB => Path.Combine(FileSystem.AppDataDirectory, "ISIDatabase.db3");

        /// <summary>
        /// Construtor privado para for�ar o padr�o singleton.
        /// </summary>
        /// <param name="dbPath">Caminho para o arquivo de banco de dados.</param>
        private Database(string dbPath)
        {
            // SharedCache removido: incompat�vel com WAL mode (causa serializa��o inesperada)
            sqlConnection = new SQLiteAsyncConnection(dbPath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
        }

        /// <summary>
        /// Obt�m a inst�ncia singleton do banco de dados, inicializando-a se necess�rio.
        /// Esta � a forma correta de acessar o banco de dados.
        /// </summary>
        /// <example>
        /// var db = await Database.GetInstanceAsync();
        /// var conexao = db.sqlConnection;
        /// </example>
        /// <returns>A inst�ncia do banco de dados inicializada.</returns>
        public static async Task<Database> GetInstanceAsync()
        {
            if (_database == null)
            {
                lock (_lockObject)
                {
                    // Double-check lock
                    _database ??= new Database(PathDB);
                }
            }

            // Garante que a inicializa��o (cria��o de tabelas) seja executada
            await _database.InitializeDatabaseAsync();

            return _database;
        }

        /// <summary>
        /// Inicializa o banco de dados (cria tabelas, etc.) de forma ass�ncrona e segura (thread-safe).
        /// </summary>
        /// <summary>
        /// Inicializa o banco de dados (configura��es de conex�o, etc.) 
        /// de forma ass�ncrona e segura (thread-safe).
        /// A cria��o de tabelas � gerenciada por 'ManutencaoTabelas'.
        /// </summary>
        private async Task InitializeDatabaseAsync()
        {
            if (_isInitialized)
                return;

            await _asyncLock.WaitAsync();
            try
            {
                if (_isInitialized)
                    return;

                // Apenas habilita o WAL. A cria��o de tabelas foi movida para ManutencaoTabelas.
                await sqlConnection.EnableWriteAheadLoggingAsync();

                _isInitialized = true;
            }
            finally
            {
                _asyncLock.Release();
            }
        }

        /// <summary>
        /// Fecha a conex�o com o banco de dados e limpa a inst�ncia singleton.
        /// </summary>
        public static async Task CloseDatabaseAsync()
        {
            if (_readConnection != null)
            {
                await _readConnection.CloseAsync();
                _readConnection = null;
            }

            if (_database?.sqlConnection != null)
            {
                await _database.sqlConnection.CloseAsync();
                lock (_lockObject)
                {
                    _database = null;
                    _isInitialized = false; // Permite reinicializar
                }
            }
        }

        /// <summary>
        /// Reabre o banco de dados. (Equivalente a chamar GetInstanceAsync).
        /// </summary>
        public static async Task ReopenDatabaseAsync()
        {
            // GetInstanceAsync j� lida com a l�gica de cria��o e inicializa��o
            await GetInstanceAsync();
        }

        /// <summary>
        /// Obt�m a conex�o de escrita pronta para uso.
        /// </summary>
        public static async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            var db = await GetInstanceAsync().ConfigureAwait(false);
            return db.sqlConnection;
        }

        // Conex�o read-only separada da de escrita.
        // Com WAL ativo na conex�o de escrita, o SQLite garante que leituras e escritas
        // em conex�es distintas n�o se bloqueiam � readers n�o ficam na fila do writer.
        private static SQLiteAsyncConnection? _readConnection;

        /// <summary>
        /// Obt�m a conex�o read-only para queries de leitura da UI.
        /// </summary>
        public static async Task<SQLiteAsyncConnection> GetReadConnectionAsync()
        {
            if (_readConnection != null) return _readConnection;

            // Garante WAL habilitado antes de abrir a segunda conex�o
            await GetInstanceAsync().ConfigureAwait(false);

            lock (_lockObject)
            {
                _readConnection ??= new SQLiteAsyncConnection(PathDB, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
            }
            return _readConnection;
        }
    }
}
