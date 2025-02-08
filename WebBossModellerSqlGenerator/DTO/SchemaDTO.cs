using WebBossModellerSqlGenerator.Models;

namespace WebBossModellerSqlGenerator.DTO
{
    // Definition der Klasse SchemaDTO, die als Data Transfer Object (DTO) verwendet wird.
    // Ein DTO wird oft verwendet, um Daten zwischen verschiedenen Schichten einer Anwendung zu transportieren.
    public class SchemaDTO
    {
        // Eigenschaft f�r den Namen des Schemas.
        // Der Getter und Setter erm�glichen das Lesen und Schreiben des Namens.
        public string Name { get; set; }

        // Eigenschaft f�r eine Liste von TableDTO-Objekten.
        // Diese Liste repr�sentiert die Tabellen, die zu diesem Schema geh�ren.
        public List<TableDTO> Tables { get; set; }

        // Statische Methode zur Konvertierung eines DbSchema-Objekts in ein SchemaDTO-Objekt.
        // Diese Methode nimmt ein DbSchema-Objekt als Parameter und gibt ein SchemaDTO-Objekt zur�ck.
        public static SchemaDTO ToDTO(DbSchema schema)
        {
            // Erstellung eines neuen SchemaDTO-Objekts.
            return new SchemaDTO
            {
                // Zuweisen des Namens des DbSchema-Objekts zum Name-Feld des SchemaDTO.
                Name = schema.Name,

                // Konvertierung der Tabellen des DbSchema-Objekts in eine Liste von TableDTO-Objekten.
                // Die Select-Methode wird verwendet, um jede Tabelle in ein TableDTO-Objekt umzuwandeln.
                // Die ToList-Methode konvertiert die resultierende Sammlung in eine Liste.
                Tables = schema.Tables.Select(t => TableDTO.ToDTO(t)).ToList()
            };
        }
    }
}
