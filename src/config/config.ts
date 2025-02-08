// Import der dotenv-Bibliothek, um Umgebungsvariablen aus einer .env-Datei zu laden
import { config } from "dotenv";

// Pfad zur .env-Datei, aus der die Umgebungsvariablen geladen werden sollen
const configFile = `./.env`;

// Lädt die Umgebungsvariablen aus der angegebenen .env-Datei
config({ path: configFile });

// Destrukturierung der Umgebungsvariablen aus process.env
const { MONGO_URI, PORT, JWT_SECRET, NODE_ENV, LDAP_URL } = process.env;

// Exportiert die Umgebungsvariablen als Konfigurationsobjekt
export default {
    MONGO_URI, // URI für die MongoDB-Verbindung
    PORT, // Port, auf dem der Server lauscht
    JWT_SECRET, // Geheimer Schlüssel für JWT (JSON Web Tokens)
    LDAP_URL, // URL für die LDAP-Verbindung
    env: NODE_ENV // Umgebungsvariable (z.B. "development", "production")
};