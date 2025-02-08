// Importiert die Server-Klasse aus dem "http"-Modul.
// Die Server-Klasse wird verwendet, um einen HTTP-Server zu erstellen.
import { Server } from "http";

// Importiert die Express-Anwendung aus der Datei "./server".
// Diese Datei enthält die Konfiguration und Routen der Anwendung.
import app from "./server";

// Importiert die Konfiguration aus der Datei "./config/config".
// Diese Datei enthält wahrscheinlich Umgebungsvariablen oder andere Konfigurationswerte,
// wie z. B. den Port, auf dem der Server laufen soll.
import config from "./config/config";

// Deklariert eine Variable "server" vom Typ "Server".
// Diese Variable wird verwendet, um den HTTP-Server zu speichern.
let server: Server;

// Startet den Server und weist ihn der Variablen "server" zu.
// Der Server hört auf dem Port, der in der Konfiguration angegeben ist (config.PORT).
server = app.listen(config.PORT, () => {
    // Gibt eine Nachricht in der Konsole aus, sobald der Server gestartet ist.
    // Die Nachricht enthält den Port, auf dem der Server läuft.
    console.log("Server is running on port " + config.PORT);
});