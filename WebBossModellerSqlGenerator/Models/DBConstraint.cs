using System.Data;

namespace WebBossModellerSqlGenerator.Models
{
    public abstract class DBConstraint
    {
        public string ConstraintName { get; set; }
        public List<string> ColumnNames { get; set; }
    }
    public class PrimaryKeyConstraint : DBConstraint
    {

    }

    public class ForeignKeyConstraint : DBConstraint
    {
        public string ReferencedTable { get; set; }
        public string ReferencedColumn { get; set; }
    }

    public class CheckConstraint : DBConstraint
    {
        public string CheckDefinition { get; set; }
    }
    public class UniqueConstraint: DBConstraint
    {

    }
}
