namespace WebBossModellerSqlGenerator.Models
{
    public interface ISql
    {
        string ToSqlForPostgresSQL(bool isCaseSensitive);
        string ToSqlForMSSSQL();

        string ToSqlForMySQL();

    }
}
