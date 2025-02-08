# Verwendet das offizielle Node.js 20 Image mit Alpine Linux als Basis.
# Alpine ist eine sehr schlanke Linux-Distribution, die für Container optimiert ist.
FROM node:20-alpine

# Legt das Arbeitsverzeichnis im Container fest.
# Alle folgenden Befehle werden in diesem Verzeichnis ausgeführt.
WORKDIR /usr/src/app

# Kopiert die Dateien `package.json` und `package-lock.json` (falls vorhanden) in das Arbeitsverzeichnis.
# Diese Dateien enthalten die Abhängigkeiten des Projekts.
COPY package*.json ./

# Installiert die Abhängigkeiten des Projekts mit npm.
# Die Abhängigkeiten werden basierend auf den kopierten `package.json`-Dateien installiert.
RUN npm install

# Kopiert den gesamten restlichen Projektcode in das Arbeitsverzeichnis.
# Dies umfasst alle Dateien und Ordner im aktuellen Verzeichnis (außer den im `.dockerignore`-Datei ausgeschlossenen).
COPY . .

# Führt den Build-Befehl des Projekts aus.
# Dieser Befehl ist in der `package.json`-Datei definiert und kompiliert oder bereitet das Projekt für die Produktion vor.
RUN npm run build

# Gibt an, dass der Container den Port 8083 exponiert.
# Dies bedeutet, dass der Container auf diesem Port lauscht, aber der Port noch nicht automatisch für den Host freigegeben ist.
EXPOSE 8083

# Definiert den Standardbefehl, der beim Starten des Containers ausgeführt wird.
# In diesem Fall wird der Befehl `npm start` ausgeführt, um die Anwendung zu starten.
CMD ["npm", "start"]