using WebBossModellerSqlGenerator.Models;

namespace WebBossModellerSqlGenerator.DTO
{
    // Definition der Klasse DatabaseDTO, die als Datenübertragungsobjekt (DTO) dient.
    public class DatabaseDTO
    {
        // Eigenschaft für den Namen der Datenbank.
        public string DatabaseName { get; set; }

        // Liste von SchemaDTO-Objekten, die die Schemas der Datenbank repräsentieren.
        public List<SchemaDTO> Schemas { get; set; }

        // Statische Methode zur Konvertierung eines DbDatabase-Objekts in ein DatabaseDTO-Objekt.
        public static DatabaseDTO ToDTO(DbDatabase dbDatabase)
        {
            // Erstellt ein neues DatabaseDTO-Objekt und setzt dessen Eigenschaften.
            return new DatabaseDTO
            {
                // Setzt den Namen der Datenbank.
                DatabaseName = dbDatabase.Name,

                // Konvertiert die Schemas der Datenbank in SchemaDTO-Objekte und fügt sie der Liste hinzu.
                Schemas = dbDatabase.Schemas.Select(s => SchemaDTO.ToDTO(s)).ToList()
            };
        }
    }
}
