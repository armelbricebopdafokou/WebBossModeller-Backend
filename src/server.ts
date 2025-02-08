// Importiert das Express-Framework und den Typ "Express" aus dem Paket "express".
// "Express" ist der Typ für die Express-Anwendung.
import express, { Express } from "express";

// Importiert die Routen für die Authentifizierung (AuthRouter) und Benutzer (UserRouter).
// Diese Routen definieren die Endpunkte für die jeweiligen Funktionalitäten.
import AuthRouter from "./route/authRoutes";
import UserRouter from "./route/userRoute";

// Importiert die Middleware-Funktionen für die Fehlerbehandlung.
// - errorConverter: Konvertiert Fehler in ein standardisiertes Format.
// - errorHandler: Verarbeitet die konvertierten Fehler und sendet eine entsprechende Antwort an den Client.
import { errorConverter, errorHandler } from "./middleware";

// Importiert die Funktion "connectDB" zur Verbindung mit der Datenbank.
// Diese Funktion stellt die Verbindung zur Datenbank her, bevor die Anwendung startet.
import { connectDB } from "./database";

// Importiert das CORS-Paket, um Cross-Origin Resource Sharing (CORS) zu ermöglichen.
// CORS ist notwendig, um Anfragen von anderen Domains zuzulassen.
import cors from 'cors';

// Erstellt eine neue Express-Anwendung.
const app: Express = express();

// LDAP Configuration
// Hier könnte die Konfiguration für die LDAP-Authentifizierung erfolgen.
// Zum Beispiel:
// passport.use(new LdapStrategy({ ... }));

// Middleware, um JSON-Daten in Anfragen zu verarbeiten.
// Ermöglicht es der Anwendung, JSON-Daten im Body von POST- oder PUT-Anfragen zu lesen.
app.use(express.json());

// Aktiviert CORS für die Anwendung.
// Ermöglicht Anfragen von anderen Domains (z. B. von einem Frontend).
app.use(cors());

// Middleware, um URL-kodierte Daten in Anfragen zu verarbeiten.
// Das `extended: true` ermöglicht die Verarbeitung von verschachtelten Objekten in den URL-kodierten Daten.
app.use(express.urlencoded({ extended: true }));

// Fügt die Authentifizierungsrouten (AuthRouter) zur Anwendung hinzu.
// Alle Anfragen, die mit den in AuthRouter definierten Pfaden übereinstimmen, werden hier verarbeitet.
app.use(AuthRouter);

// Fügt die Benutzerrouten (UserRouter) zur Anwendung hinzu.
// Alle Anfragen, die mit den in UserRouter definierten Pfaden übereinstimmen, werden hier verarbeitet.
app.use(UserRouter);

// Fügt die Fehlerkonvertierungs-Middleware zur Anwendung hinzu.
// Diese Middleware wird verwendet, um Fehler in ein standardisiertes Format zu konvertieren.
app.use(errorConverter);

// Fügt die Fehlerbehandlungs-Middleware zur Anwendung hinzu.
// Diese Middleware verarbeitet die konvertierten Fehler und sendet eine entsprechende Antwort an den Client.
app.use(errorHandler);



// Passport LDAP strategy configuration
// Hier könnte die Konfiguration der LDAP-Strategie für Passport erfolgen.
// Zum Beispiel:
// passport.use(new LdapStrategy({ ... }));

// Stellt die Verbindung zur Datenbank her.
// Diese Funktion wird aufgerufen, um sicherzustellen, dass die Anwendung eine Verbindung zur Datenbank hat, bevor sie Anfragen bearbeitet.
connectDB();

// Exportiert die Express-Anwendung, damit sie in anderen Dateien verwendet werden kann.
// Zum Beispiel in der Datei, die den Server startet.
export default app;