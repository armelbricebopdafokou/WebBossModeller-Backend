import {config} from "dotenv"

const configFile = `./.env`;

config({path: configFile});

const {MONGO_URI, PORT, JWT_SECRET, NODE_ENV, LDAP_URL} = process.env

export default {
    MONGO_URI, 
    PORT, 
    JWT_SECRET, 
    LDAP_URL,
    env:NODE_ENV
}