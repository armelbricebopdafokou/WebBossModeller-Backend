import express, { Express } from "express";
import { Server } from "http";
import AuthRouter from "./route/authRoutes";
import UserRouter from "./route/userRoute";
import { errorConverter, errorHandler } from "./middleware";
import { connectDB } from "./database";
import config from "./config/config";
import cors from 'cors';

const app:Express = express();
let server: Server;

app.use(express.json());
app.use(cors());
app.use(express.urlencoded({extended:true}));
app.use(AuthRouter);
app.use(UserRouter);
app.use(errorConverter);
app.use(errorHandler);

connectDB();

server = app.listen(config.PORT, ()=> {
    console.log("Server is running on port "+ config.PORT)
})