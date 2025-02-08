// Import der notwendigen Module
import mongoose from "mongoose"; // Mongoose für die Verbindung zur MongoDB
import config from "../config/config"; // Konfigurationsdatei, die die MongoDB-Verbindungs-URI enthält

// Definition der Funktion connectDB, die asynchron eine Verbindung zur Datenbank herstellt
export const connectDB = async () => {
    try {
        // Loggt eine Nachricht, dass versucht wird, eine Verbindung zur Datenbank herzustellen
        console.info("connecting to database..." + config.MONGO_URI);

        // Stellt die Verbindung zur MongoDB-Datenbank her
        // config.MONGO_URI! stellt sicher, dass der Wert nicht null oder undefined ist
        await mongoose.connect(config.MONGO_URI!);

        // Loggt eine Nachricht, dass die Verbindung erfolgreich hergestellt wurde
        console.info("Database connected");
    } catch (error) {
        // Fängt Fehler ab, die während des Verbindungsversuchs auftreten können
        console.error(error);

        // Beendet den Prozess mit einem Fehlercode, wenn die Verbindung fehlschlägt
        process.exit(1);
    }
};