using WebBossModellerSqlGenerator.Models;

namespace WebBossModellerSqlGenerator.DTO
{
    public class GraphicDTO
    {
        // Name der Datenbank
        public string DatabaseName { get; set; }

        // Name des Schemas
        public string SchemaName { get; set; }

        // Array von Tabellen (enthält mehrere Tabellenobjekte)
        public TableDTO[] tables { get; set; }

        // Gibt an, ob die Datenbank zwischen Groß- und Kleinschreibung unterscheidet
        public bool IsCaseSensitive { get; set; }
    }

}
