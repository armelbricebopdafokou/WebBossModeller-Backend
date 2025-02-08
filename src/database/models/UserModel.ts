// Import der notwendigen Module von mongoose und validator
import mongoose, { Schema, Document} from "mongoose"
import validator, { trim } from "validator";

// Definition der IUser-Schnittstelle, die die Struktur eines Benutzerdokuments beschreibt
export interface IUser extends Document{
    lastName: string; // Nachname des Benutzers
    firstName: string; // Vorname des Benutzers
    email: string; // E-Mail-Adresse des Benutzers
    password: string; // Passwort des Benutzers
    graphics: [any]; // Array für Grafiken (Typ any, da der genaue Typ nicht spezifiziert ist)
    createdAt: Date; // Datum der Erstellung des Benutzers
    updatedAt: Date; // Datum der letzten Aktualisierung des Benutzers
}

// Definition des User-Schemas, das die Struktur der Benutzerdokumente in der Datenbank beschreibt
const UserSchema: Schema = new Schema(
    {
        lastName: {
            type: String, // Der Nachname ist ein String
            trim: true, // Leerzeichen am Anfang und Ende werden entfernt
            require: [true, "Name must be provided"], // Nachname ist ein Pflichtfeld
            minlength: 3 // Der Nachname muss mindestens 3 Zeichen lang sein
        },
        firstName: {
            type: String, // Der Vorname ist ein String
            trim: true, // Leerzeichen am Anfang und Ende werden entfernt
            minlength: 3 // Der Vorname muss mindestens 3 Zeichen lang sein
        },
        email: {
            type: String, // Die E-Mail-Adresse ist ein String
            required: true, // E-Mail ist ein Pflichtfeld
            unique: true, // Die E-Mail-Adresse muss eindeutig sein
            trim: true, // Leerzeichen am Anfang und Ende werden entfernt
            validate: [validator.isEmail, "Please provide a valid email."] // Validierung, ob es sich um eine gültige E-Mail-Adresse handelt
        },
        password: {
            type: String, // Das Passwort ist ein String
            trim: false, // Leerzeichen am Anfang und Ende werden nicht entfernt
            require: [true, "Password must be provided"], // Passwort ist ein Pflichtfeld
            minlength: 8 // Das Passwort muss mindestens 8 Zeichen lang sein
        },
        graphics: {
            type: Array, // Die Grafiken werden als Array gespeichert
            require: false // Grafiken sind kein Pflichtfeld
        }
    },
    {
        timestamps: true // Automatisches Hinzufügen von createdAt und updatedAt Feldern
    }
);

// Erstellung des Mongoose-Modells für die Benutzer-Entität
const User = mongoose.model<IUser>("User", UserSchema);

// Export des Modells, damit es in anderen Teilen der Anwendung verwendet werden kann
export default User;