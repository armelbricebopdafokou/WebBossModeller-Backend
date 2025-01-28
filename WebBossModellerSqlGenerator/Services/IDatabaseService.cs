using WebBossModellerSqlGenerator.Models;

public interface IDatabaseService
{
    DbDatabase GetDatabaseSchema(string connectionString, string schemaName);
}