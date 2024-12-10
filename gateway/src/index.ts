import express from "express";
import proxy from "express-http-proxy";
import cors from 'cors';

const app = express();

app.use(cors());
app.use(express.json());
app.use(express.urlencoded({extended: true}));



const auth = proxy("http://localhost:8083")
const sqlGenerator = proxy("http://localhost:8085");
//process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";
app.use("/users",auth );
app.use("/sql", sqlGenerator);



const server=app.listen(82, ()=>{
    console.log("Gateway is Listening to Port 8082");
});

const exitHandler = () => {
    if (server) {
        server.close(() => {
            console.info("Server closed");
            process.exit(1);
        });
    } else {
        process.exit(1);
    }
};

const unexpectedErrorHandler = (error: unknown) => {
    console.error(error);
    //exitHandler();
};

process.on("uncaughtException", unexpectedErrorHandler);
process.on("unhandledRejection", unexpectedErrorHandler);
