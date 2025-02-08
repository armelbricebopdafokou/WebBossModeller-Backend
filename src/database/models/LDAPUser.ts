// Import der notwendigen Module von mongoose und validator
import mongoose, { Schema, Document } from "mongoose";
import validator, { trim } from "validator";

// Definition der ILDAPUser-Schnittstelle, die die Struktur eines LDAP-Benutzerdokuments beschreibt
export interface ILDAPUser extends Document {
    username: string; // Benutzername des LDAP-Benutzers
    password: string; // Passwort des LDAP-Benutzers
    graphics: [any]; // Array für Grafiken (Typ any, da der genaue Typ nicht spezifiziert ist)
    createdAt: Date; // Datum der Erstellung des LDAP-Benutzers
    updatedAt: Date; // Datum der letzten Aktualisierung des LDAP-Benutzers
}

// Definition des LDAPUser-Schemas, das die Struktur der LDAP-Benutzerdokumente in der Datenbank beschreibt
const LDAPUserSchema: Schema = new Schema(
    {
        username: {
            type: String, // Der Benutzername ist ein String
            trim: true, // Leerzeichen am Anfang und Ende werden entfernt
            require: [true, "Name must be provided"], // Benutzername ist ein Pflichtfeld
            minlength: 3 // Der Benutzername muss mindestens 3 Zeichen lang sein
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

// Erstellung des Mongoose-Modells für die LDAP-Benutzer-Entität
const LDapUser = mongoose.model<ILDAPUser>("LDapUser", LDAPUserSchema);

// Export des Modells, damit es in anderen Teilen der Anwendung verwendet werden kann
export default LDapUser;