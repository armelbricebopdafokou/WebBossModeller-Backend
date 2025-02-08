// Importiert die Router-Klasse aus dem Express-Framework.
// Router wird verwendet, um Routen zu definieren und Anfragen an bestimmte Endpunkte zu handhaben.
import { Router } from "express";

// Importiert den UserController, der die Geschäftslogik für Benutzeraktionen enthält.
// In diesem Fall werden Methoden zum Speichern und Abrufen von Grafiken bereitgestellt.
import UserController from "../controllers/UserController";

// Importiert das authMiddleware, das sicherstellt, dass nur authentifizierte Benutzer auf bestimmte Routen zugreifen können.
// Dieses Middleware wird vor der Ausführung der Controller-Methoden ausgeführt.
import { authMiddleware } from "../middleware/index";

// Erstellt eine neue Instanz eines Express-Routers.
const userRouter = Router();

// Definiert eine POST-Route unter dem Pfad "/graphics".
// Bevor die saveGraphics-Methode des UserControllers aufgerufen wird, wird das authMiddleware ausgeführt,
// um sicherzustellen, dass der Benutzer authentifiziert ist.
userRouter.post("/graphics", authMiddleware, UserController.saveGraphics);

// Definiert eine GET-Route unter dem Pfad "/graphics".
// Auch hier wird das authMiddleware verwendet, um sicherzustellen, dass der Benutzer authentifiziert ist,
// bevor die getGraphics-Methode des UserControllers aufgerufen wird.
userRouter.get("/graphics", authMiddleware, UserController.getGraphics);

// Die folgenden Zeilen sind auskommentiert und werden derzeit nicht verwendet.
// Sie könnten als Beispiele oder für zukünftige Implementierungen dienen.
// userRouter.post("/graphics", UserController.saveGraphics)
// userRouter.post("/graphics", UserController.saveGraphics);

// Exportiert den userRouter, damit er in anderen Teilen der Anwendung verwendet werden kann.
export default userRouter;