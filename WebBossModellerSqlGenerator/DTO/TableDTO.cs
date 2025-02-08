using WebBossModellerSqlGenerator.Models;

namespace WebBossModellerSqlGenerator.DTO
{
    // Definition der Klasse TableDTO, die als Datenübertragungsobjekt (DTO) für Tabellen dient.
    public class TableDTO
    {
        // Eigenschaft für den Klassennamen der Tabelle.
        public string ClassName { get; set; }

        // Eigenschaft, die angibt, ob die Tabelle eine schwache Entität ist.
        public bool IsWeak { get; set; }

        // Liste von ColumnDTO-Objekten, die die Spalten der Tabelle repräsentieren.
        public List<ColumnDTO> Columns { get; set; }

        // Optionale Liste von ColumnDTO-Objekten, die eine eindeutige Kombination von Spalten darstellt.
        public HashSet<List<ColumnDTO>>? UniqueCombination { get; set; }

        public void AddUniqueCombination(HashSet<List<DbColumn>>? uniqueCombination)
        {
            UniqueCombination = new HashSet<List<ColumnDTO>>();
            foreach (var listColumn in uniqueCombination)
            {
                UniqueCombination.Add(listColumn.Select(uc => ColumnDTO.ToDTO(uc)).ToList());
            }
           
        }

        // Statische Methode zur Konvertierung eines DbTable-Objekts in ein TableDTO-Objekt.
        public static TableDTO ToDTO(DbTable table)
        {
            // Erstellt ein neues TableDTO-Objekt und setzt dessen Eigenschaften.
            TableDTO tableDTO = new TableDTO
            {
                // Setzt den Klassennamen der Tabelle.
                ClassName = table.Name,

                // Setzt den Wert, der angibt, ob die Tabelle eine schwache Entität ist.
                IsWeak = table.IsWeak,

                // Konvertiert die Spalten der Tabelle in ColumnDTO-Objekte und fügt sie der Liste hinzu.
                Columns = table.Columns.Select(c => ColumnDTO.ToDTO(c)).ToList(),
               
            };

            //konvertiert die Unique combination Spalten in Unique Combination DTO

             tableDTO.AddUniqueCombination(table.UniqueCombination); 

            return tableDTO;
        }
    }
}
