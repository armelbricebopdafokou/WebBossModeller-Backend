// Import der notwendigen Module
import { Router } from "express"; // Express-Router für die Definition von Routen
import AuthController from "../controllers/AuthController"; // Controller für Authentifizierungslogik

// Erstellung eines Express-Routers
const authRouter = Router();

// Definition der Routen und Zuordnung zu den entsprechenden Controller-Methoden
authRouter.post("/register", AuthController.register); // Route für die Benutzerregistrierung
authRouter.post("/login", AuthController.login); // Route für das Benutzerlogin
authRouter.post("/ldap", AuthController.loginLDAP); // Route für das LDAP-Login

// Export des Routers, damit er in anderen Teilen der Anwendung verwendet werden kann
export default authRouter;