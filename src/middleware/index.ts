import { ErrorRequestHandler, NextFunction, Request, Response } from "express";
import { ApiError } from "../utils";
import config from "../config/config";
import jwt from "jsonwebtoken";
import validator from "validator";

// Error Converter Middleware
export const errorConverter: ErrorRequestHandler = (err, req, res, next) => {
    let error = err;

    if (!(error instanceof ApiError)) {
        const statusCode = error.statusCode || (error instanceof Error ? 400 : 500);
        const message =
            error.message ||
            (statusCode === 400 ? "Bad Request" : "Internal Server Error");
        error = new ApiError(statusCode, message, false, err.stack.toString());
    }
    next(error);
};

// Error Handler Middleware
export const errorHandler: ErrorRequestHandler = (err, req, res, next) => {
    let { statusCode, message } = err;

    // In production, hide non-operational errors
    if (process.env.NODE_ENV === "production" && !err.isOperational) {
        statusCode = 500; // Internal Server Error
        message = "Internal Server Error";
    }

    res.locals.errorMessage = err.message;

    const response = {
        code: statusCode,
        message,
        ...(process.env.NODE_ENV === "development" && { stack: err.stack }),
    };

    // Log errors in development
    if (process.env.NODE_ENV === "development") {
        console.error(err);
    }

    res.status(statusCode).json(response);
};

// JWT Payload Interface
interface TokenPayload {
    id: string;
    exp: number;
}

const jwtSecret = config.JWT_SECRET as string;

// Authentication Middleware
export const authMiddleware = async (
    req: Request,
    res: Response,
    next: NextFunction
) => {
    const authHeader = req.headers.authorization;

    // Check if the Authorization header exists
    if (!authHeader) {
        return next(new ApiError(401, "Missing authorization header"));
    }

    // Extract the token from the Bearer format
    const [, token] = authHeader.split(" ");
    if (!token) {
        return next(new ApiError(401, "Invalid token format"));
    }

    try {
        // Verify the token
        const decoded = jwt.verify(token, jwtSecret) as TokenPayload;

        // Check if the token has expired
        if (!decoded.exp || decoded.exp < Date.now() / 1000) {
            return next(new ApiError(401, "Token has expired"));
        }

        // Attach user information to the request object
        const id = decoded.id;
        if (isEmailValid(id)) {
            req.body.email = id;
        } else {
            req.body.username = id;
        }

        return next();
    } catch (error) {
        console.error(error);
        return next(new ApiError(401, "Invalid token"));
    }
};

// Email Validation Utility
function isEmailValid(email: string) {
    return validator.isEmail(email);
}