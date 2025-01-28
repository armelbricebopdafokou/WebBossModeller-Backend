import { ErrorRequestHandler, NextFunction, Request, Response } from "express";
import { ApiError } from "../utils";
import { IUser } from "../database";
import config from "../config/config";
import jwt from "jsonwebtoken";

export const errorConverter : ErrorRequestHandler = (err, req, res, next) =>{
    let error = err;

    if(!(error instanceof ApiError))
    {
        const statusCode = error.statusCode || (error instanceof Error ? 400 : 500);
        const message =
            error.message ||
            (statusCode === 400 ? "Bad Request" : "Internal Server Error");
        error = new ApiError(statusCode, message, false, err.stack.toString());
    }
    next(error);
};

export const errorHandler: ErrorRequestHandler = (err, req, res, next) =>{
    let { statusCode, message} = err;
    if(process.env.NODE_ENV === "production" && !err.isOperational)
    {
        statusCode = 500; //internal server error
        message = "Internal Server Error";
    }
    res.locals.errorMessage = err.message;

    const response = {
        code: statusCode,
        message,
        ...(process.env.NODE_ENV === "development" && { stack: err.stack }),
    };

    if (process.env.NODE_ENV === "development") {
        console.error(err);
    }

    res.status(statusCode).json(response);
    next();
};


interface TokenPayload {
    id: string;
    exp: number;
}

const jwtSecret = config.JWT_SECRET as string;

export const authMiddleware = async (
    req: Request,
    res: Response,
    next: NextFunction
) => {
    const authHeader = req.headers.authorization;
    if (!authHeader) {
        return next(new ApiError(401, "Missing authorization header"));
    }
    
    //const [, token] = authHeader.split(" ");
    
    try {
       
        const decoded = jwt.verify(authHeader, jwtSecret) as TokenPayload;
        
        if(!decoded.exp || decoded.exp == 0) return next(new ApiError(401, "Token has been expired"));

        let id = decoded.id

        if(isEmailValid(id) == false)
        {
            req.body.username = id
        }else{
            req.body.email = id
        }
        
        return next();
    } catch (error) {
        console.error(error);
        return next(new ApiError(401, "Invalid token"));
    }
};

var emailRegex = /^[-!#$%&'*+\/0-9=?A-Z^_a-z{|}~](\.?[-!#$%&'*+\/0-9=?A-Z^_a-z`{|}~])*@[a-zA-Z0-9](-*\.?[a-zA-Z0-9])*\.[a-zA-Z](-?[a-zA-Z0-9])+$/;

function isEmailValid(email:string) {
    if (!email)
        return false;

    if(email.length>254)
        return false;

    var valid = emailRegex.test(email);
    if(!valid)
        return false;

    // Further checking of some things regex can't handle
    var parts = email.split("@");
    if(parts[0].length>64)
        return false;

    var domainParts = parts[1].split(".");
    if(domainParts.some(function(part:any) { return part.length>63; }))
        return false;

    return true;
}