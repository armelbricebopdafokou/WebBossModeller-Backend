from ldap3 import Server, Connection, SAFE_SYNC
from ldap3.core.exceptions import *

def check_ldap(un: str, pw: str) -> (bool,str):
    result = False
    resultText:str = "None"
    ldaprdn = f"uid={un},ou=People,o=hs-bochum.de,o=isp"
    try:
        server = Server('ods0.hs-bochum.de', port = 636, use_ssl = True)
        conn = Connection(server, ldaprdn, pw, client_strategy=SAFE_SYNC, auto_bind=True)
        result = True
        resultText = "Anmeldeinformation OK"
    except LDAPBindError as e:
        print("LDAP-Bind-Fehler: ",e)
        result = False
        resultText = "Falsche Anmeldeinformationen!"
    except LDAPSocketOpenError as e:
        print("Fehler beim Öffnen der Serververbindung: ",e)
        result = False
        resultText = "Fehler beim Öffnen der Serververbindung"
    #status, result, response, _ = conn.search('o=test', '(objectclass=*)') # usually you don't need the original request (4th element of the returned tuple)
    return result,resultText

if __name__ == "__main__":
 a,b = check_ldap("brbo9264","12345S@leil")
 print(a,b)
