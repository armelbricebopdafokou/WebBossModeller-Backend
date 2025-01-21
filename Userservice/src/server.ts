import express, { Express } from "express";

import AuthRouter from "./route/authRoutes";
import UserRouter from "./route/userRoute";
import { errorConverter, errorHandler } from "./middleware";
import { connectDB } from "./database";

import cors from 'cors';
import passport from 'passport';
import  LdapStrategy  = require('passport-ldapauth');

const app:Express = express();


// LDAP Configuration


app.use(express.json());
app.use(cors());
app.use(express.urlencoded({extended:true}));
app.use(AuthRouter);
app.use(UserRouter);
app.use(errorConverter);
app.use(errorHandler);
app.use(passport.initialize());
// Passport LDAP strategy configuration


connectDB();

export default app;