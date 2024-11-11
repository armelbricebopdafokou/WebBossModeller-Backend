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
    email: string;
    lastName: string;
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
    
    const [, token] = authHeader.split(" ");
    
    try {
       
        const decoded = jwt.verify(token, jwtSecret) as TokenPayload;
       
        if(!decoded.exp || decoded.exp == 0) return next(new ApiError(401, "Token has been expired"));
        
        return next();
    } catch (error) {
        console.error(error);
        return next(new ApiError(401, "Invalid token"));
    }
};
