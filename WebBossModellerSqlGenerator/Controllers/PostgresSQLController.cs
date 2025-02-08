using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using WebBossModellerSqlGenerator.DTO;
using WebBossModellerSqlGenerator.Models;

namespace WebBossModellerSqlGenerator.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class PostgresSQLController : ControllerBase
    {
        // Privates Feld zur Speicherung des PostgreSQL-Datenbankdienstes.
        // Dieses Feld wird über den Konstruktor injiziert (Dependency Injection).
        private readonly PostgreSQLDatabaseService _postgresqlService;

        // Privates Feld für den Logger.
        private readonly ILogger<PostgresSQLController> _logger;

        // Konstruktor, der eine Instanz von PostgreSQLDatabaseService über Dependency Injection erhält.
        // Dies ermöglicht die lose Kopplung und einfachere Testbarkeit des Controllers.
        public PostgresSQLController(PostgreSQLDatabaseService postgresqlService, ILogger<PostgresSQLController> logger)
        {
            _postgresqlService = postgresqlService;
            _logger = logger;
        }

        // HTTP GET-Methode, die aufgerufen wird, um eine Datenbank zu erstellen.
        // Der Methodenname ist "CreateDatabase", was darauf hindeutet, dass sie eine Datenbank erstellt.
        // Die Methode gibt ein ActionResult vom Typ DbDatabase zurück.
        [HttpGet]
        public async Task<ActionResult<DbDatabase>> CreateDatabase(string dbName, bool isCaseSensitive = false)
        {
            // Überprüfung, ob der Datenbankname (dbName) null oder leer ist.
            // Falls ja, wird ein BadRequest-Resultat mit einer Fehlermeldung zurückgegeben.
            if (string.IsNullOrWhiteSpace(dbName))
                return BadRequest("Parameter Name is missing");

            // Erstellung einer neuen Instanz der DbDatabase-Klasse mit dem übergebenen Datenbanknamen.
            DbDatabase db = new DbDatabase(dbName);

            // Rückgabe eines OK-Resultats mit dem SQL-Statement, das von der ToSqlForPostgresSQL-Methode generiert wird.
            // Die Methode ToSqlForPostgresSQL erzeugt das SQL-Statement zur Erstellung der Datenbank in PostgreSQL.
            // Der Parameter isCaseSensitive bestimmt, ob die Datenbank case-sensitive ist oder nicht.
            return Ok(db.ToSqlForPostgresSQL(isCaseSensitive));
        }

        [HttpGet]
        public async Task<ActionResult<DbSchema>> CreateSchema(string dbSchema, bool isCaseSensitive = false)
        {
            // Überprüfung, ob der Schemaname (dbSchema) null oder leer ist.
            if (string.IsNullOrWhiteSpace(dbSchema))
            {
                _logger.LogWarning("CreateSchema called with missing or empty dbSchema parameter.");
                return BadRequest("Parameter Name is missing");
            }

            try
            {
                // Erstellung einer neuen Instanz der DbSchema-Klasse mit dem übergebenen Schemanamen.
                DbSchema db = new DbSchema(dbSchema);

                // Generierung des SQL-Statements zur Erstellung des Schemas.
                string sqlStatement = db.ToSqlForPostgresSQL(isCaseSensitive);

                // Rückgabe des generierten SQL-Statements als HTTP-OK-Antwort.
                return Ok(sqlStatement);
            }
            catch (Exception ex)
            {
                // Loggen von unerwarteten Fehlern.
                _logger.LogError(ex, "An error occurred while generating the SQL statement for schema creation.");
                return StatusCode(500, $"An internal error occurred:{ex.Message}");
            }
        }

        [HttpPost]
        public ActionResult<string> GenerateSqlForGraphic([FromBody]GraphicDTO graphicDTO)
        {
            try
            {
                bool isCaseSensitive = graphicDTO.IsCaseSensitive;
                StringBuilder sb = new StringBuilder();
                DbDatabase dbDatabase = new DbDatabase(graphicDTO.DatabaseName);
                sb.Append(dbDatabase.ToSqlForPostgresSQL(isCaseSensitive));
                DbSchema dbSchema = new DbSchema(graphicDTO.SchemaName);
                sb.Append(dbSchema.ToSqlForPostgresSQL(isCaseSensitive));
                sb.Append(isCaseSensitive
                                        ? $"SET search_path TO \"{dbSchema.Name}\"; \n"
                                        : $"SET search_path TO {dbSchema.Name}; \n");

                // Use a dictionary to store and look up tables by name
                Dictionary<string, DbTable> tablesDict = new Dictionary<string, DbTable>();
                foreach (var elt in graphicDTO.tables)
                {
                    // sb.Append(elt.ToSqlForPostgresSQL(isCaseSensitive));


                    DbTable table = new DbTable
                    {
                        Name = elt.ClassName,
                        Schema = dbSchema,
                        Columns = elt.Columns.Select(col => new DbColumn
                        {
                            Name = col.Name,
                            Type = col.Type,
                            DefaultValue = col.DefaultValue,
                            IsNull = !(col.NotNull ?? false),
                            IsPrimaryKey = col.IsKey,
                            IsForeignKey = col.IsForeignKey,
                            CheckConstraint = col.CheckValue,
                            IsUnique = col.IsUnique ?? false,
                            ReferenceColumn = col.ReferenceColumn,
                            ReferenceTable = new DbTable() { Name = col.ReferenceTable }  // Initialize ReferenceTable as null
                        }).ToList(),
                       
                    };
                    //Übertragung von UniqueKombination
                    foreach(var cDTOList in elt.UniqueCombination)
                    {
                        table.UniqueCombination.Add(cDTOList.Select(cDTO => cDTO.ToDBColum()).ToList());
                    }

                    // Store the table by name
                    tablesDict.Add(table.Name, table);
                }

                // Update ReferenceTable properties only when IsForeignKey is true
                foreach (var table in tablesDict.Values)
                {
                    foreach (var column in table.Columns)
                    {
                        if (column.IsForeignKey && !string.IsNullOrEmpty(column.ReferenceTable.Name))
                        {
                            tablesDict.TryGetValue(column.ReferenceTable.Name, out DbTable refTable);
                            column.ReferenceTable = refTable;
                        }
                    }
                }

                // Append updated SQL statements if any reference tables were null initially
                foreach (var table in tablesDict.Values)
                {
                    sb.Append(table.ToSqlForPostgresSQL(isCaseSensitive) + "\n");
                    sb.Append(table.AddConstraintsPostgres(isCaseSensitive) + "\n\n\n");

                }


                return Ok(sb.ToString());
            }
            catch (Exception ex)
            {
                // Loggen von unerwarteten Fehlern.
                _logger.LogError(ex, "An error occurred while generating the SQL statement for a diagram.");
                return StatusCode(500, $"An internal error occurred:{ex.Message}");
            }
           
        }

        [HttpGet]
        public async Task<ActionResult<DatabaseDTO>> GetDiagramFromDatabase(string host, string dbName, string schemaName, string dbUser, string dbPassword)
        {
            // Überprüfung, ob der Hostname (host) null oder leer ist.
            if (string.IsNullOrWhiteSpace(host))
            {
                _logger.LogWarning("GetDiagramFromDatabase called with missing or empty host parameter.");
                return BadRequest("Parameter Hostname is missing");
            }

            // Überprüfung, ob der Datenbankname (dbName) null oder leer ist.
            if (string.IsNullOrWhiteSpace(dbName))
            {
                _logger.LogWarning("GetDiagramFromDatabase called with missing or empty dbName parameter.");
                return BadRequest("Parameter Database Name is missing");
            }

            // Überprüfung, ob der Schemaname (schemaName) null oder leer ist.
            if (string.IsNullOrWhiteSpace(schemaName))
            {
                _logger.LogWarning("GetDiagramFromDatabase called with missing or empty schemaName parameter.");
                return BadRequest("Parameter Schema Name is missing");
            }

            // Überprüfung, ob das Benutzerpasswort (dbPassword) null oder leer ist.
            if (string.IsNullOrWhiteSpace(dbPassword))
            {
                _logger.LogWarning("GetDiagramFromDatabase called with missing or empty dbPassword parameter.");
                return BadRequest("Parameter user password is missing");
            }

            // Überprüfung, ob der Benutzername (dbUser) null oder leer ist.
            if (string.IsNullOrWhiteSpace(dbUser))
            {
                _logger.LogWarning("GetDiagramFromDatabase called with missing or empty dbUser parameter.");
                return BadRequest("Parameter User Name is missing");
            }

            try
            {
                // Erstellung des Connection Strings für die Verbindung zur PostgreSQL-Datenbank.
                string connectionString = $"User ID={dbUser};Password={dbPassword};Host={host};Port=5432;Database={dbName};";

                // Asynchroner Aufruf der Methode GetDatabaseSchema im _postgresqlService.
                var db =  _postgresqlService.GetDatabaseSchema(connectionString, schemaName);

                // Konvertierung des Datenbankschemas in ein DTO (Data Transfer Object).
                var dbDTO = DatabaseDTO.ToDTO(db);

                // Loggen der erfolgreichen Abfrage (optional, für Debugging-Zwecke).
                _logger.LogInformation("Successfully retrieved database schema for database: {DbName}, schema: {SchemaName}", dbName, schemaName);

                // Rückgabe des DTO als HTTP-OK-Antwort.
                return Ok(dbDTO);
            }
            catch (Exception ex)
            {
                // Loggen von unerwarteten Fehlern.
                _logger.LogError(ex, "An error occurred while retrieving the database schema for database: {DbName}, schema: {SchemaName}", dbName, schemaName);
                return BadRequest(ex.Message);
            }
        }

        // HTTP GET-Methode, die aufgerufen wird, um eine Liste aller Datenbanken auf einem PostgreSQL-Server abzurufen.
        // Der Methodenname ist "AllDatabases", was darauf hindeutet, dass sie alle Datenbanken zurückgibt.
        // Die Methode gibt ein ActionResult vom Typ string[] zurück, das die Liste der Datenbanken enthält.

        [HttpGet]
        public async Task<ActionResult<string[]>> AllDatabases(string host, string dbUser, string dbPassword)
        {
            // Überprüfung, ob der Hostname (host) null oder leer ist.
            if (string.IsNullOrWhiteSpace(host))
            {
                _logger.LogWarning("AllDatabases called with missing or empty host parameter.");
                return BadRequest("Parameter Hostname is missing");
            }

            // Überprüfung, ob das Benutzerpasswort (dbPassword) null oder leer ist.
            if (string.IsNullOrWhiteSpace(dbPassword))
            {
                _logger.LogWarning("AllDatabases called with missing or empty dbPassword parameter.");
                return BadRequest("Parameter user password is missing");
            }

            // Überprüfung, ob der Benutzername (dbUser) null oder leer ist.
            if (string.IsNullOrWhiteSpace(dbUser))
            {
                _logger.LogWarning("AllDatabases called with missing or empty dbUser parameter.");
                return BadRequest("Parameter User Name is missing");
            }

            try
            {
                // Erstellung des Connection Strings für die Verbindung zur PostgreSQL-Datenbank.
                string connectionString = $"User ID={dbUser};Password={dbPassword};Host={host};Port=5432;Database=postgres;";

                // Asynchroner Aufruf der Methode GetDatabaseList im _postgresqlService.
                var db =  _postgresqlService.GetDatabaseList(connectionString);

                // Loggen der erfolgreichen Abfrage (optional, für Debugging-Zwecke).
                _logger.LogInformation("Successfully retrieved database list from host: {Host}", host);

                // Rückgabe der Liste der Datenbanken als HTTP-OK-Antwort.
                return Ok(db);
            }
            catch (Exception ex)
            {
                // Loggen von unerwarteten Fehlern.
                _logger.LogError(ex, "An error occurred while retrieving the database list from host: {Host}", host);
                return StatusCode(500, $"An internal error occurred:{ex.Message}");
            }
        }

        [HttpGet]
        public async Task<ActionResult<string[]>> AllSchema(string host, string dbUser, string dbPassword, string dbName)
        {
            // Überprüfung, ob der Hostname (host) null oder leer ist.
            if (string.IsNullOrWhiteSpace(host))
            {
                _logger.LogWarning("AllSchema called with missing or empty host parameter.");
                return BadRequest("Parameter Hostname is missing");
            }

            // Überprüfung, ob das Benutzerpasswort (dbPassword) null oder leer ist.
            if (string.IsNullOrWhiteSpace(dbPassword))
            {
                _logger.LogWarning("AllSchema called with missing or empty dbPassword parameter.");
                return BadRequest("Parameter user password is missing");
            }

            // Überprüfung, ob der Datenbankname (dbName) null oder leer ist.
            if (string.IsNullOrWhiteSpace(dbName))
            {
                _logger.LogWarning("AllSchema called with missing or empty dbName parameter.");
                return BadRequest("Parameter Database Name is missing");
            }

            // Überprüfung, ob der Benutzername (dbUser) null oder leer ist.
            if (string.IsNullOrWhiteSpace(dbUser))
            {
                _logger.LogWarning("AllSchema called with missing or empty dbUser parameter.");
                return BadRequest("Parameter User Name is missing");
            }

            try
            {
                // Erstellung des Connection Strings für die Verbindung zur PostgreSQL-Datenbank.
                string connectionString = $"User ID={dbUser};Password={dbPassword};Host={host};Port=5432;Database={dbName};";

                // Asynchroner Aufruf der Methode GetSchemaListFromDatabase im _postgresqlService.
                var db =  _postgresqlService.GetSchemaListFromDatabase(connectionString, dbName);

                // Loggen der erfolgreichen Abfrage (optional, für Debugging-Zwecke).
                _logger.LogInformation("Successfully retrieved schema list for database: {DbName}", dbName);

                // Rückgabe der Liste der Schemas als HTTP-OK-Antwort.
                return Ok(db);
            }
            catch (Exception ex)
            {
                // Loggen von unerwarteten Fehlern.
                _logger.LogError(ex, "An error occurred while retrieving the schema list for database: {DbName}", dbName);
                return StatusCode(500, $"An internal error occurred:{ex.Message}");
            }
        }


    }
}
