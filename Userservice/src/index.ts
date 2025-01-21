import { Server } from "http";
import app from "./server";
import config from "./config/config";

let server: Server;
server = app.listen(config.PORT, ()=> {
    console.log("Server is running on port "+ config.PORT)
})