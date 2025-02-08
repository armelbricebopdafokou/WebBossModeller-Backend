import express, { Request, Response } from "express";
import Jwt from "jsonwebtoken";
import { User, LDapUser, ILDAPUser } from "../database";
import config from "../config/config";
import { ApiError, encryptPassword, isPasswordMatch } from "../utils";
import { authenticate } from 'ldap-authentication';

// JWT Secret from configuration
const jwtSecret = config.JWT_SECRET as string;

// Cookie expiration settings
const COOKIE_EXPIRATION_DAYS = 90; // Cookie expiration in days
const expirationDate = new Date(Date.now() + COOKIE_EXPIRATION_DAYS * 24 * 60 * 60 * 1000);

const cookieOptions = {
    expires: expirationDate,
    secure: false, // Set to true in production for HTTPS
    httpOnly: true // Prevents client-side JavaScript from accessing the cookie
};

// LDAP Configuration
const LDAP_OPTS = {
    server: {
        url: 'ldaps://ods0.hs-bochum.de:636', // LDAP server URL
        searchBase: 'ou=People,o=hs-bochum.de,o=isp', // LDAP search base
    }
};

/**
 * Registers a new user.
 * @param req - Express request object containing user details.
 * @param res - Express response object.
 */
const register = async (req: Request, res: Response) => {
    try {
        const { lastName, firstName, email, password } = req.body;

        // Check if the user already exists
        const userExists = await User.findOne({ email });
        if (userExists) {
            throw new ApiError(400, "User already exists!");
        }

        // Create a new user with encrypted password
        const user = await User.create({
            lastName,
            firstName,
            email,
            password: await encryptPassword(password)
        });

        // Prepare user data to return
        const userData = {
            id: user._id,
            lastName: user.lastName,
            firstName: user.firstName,
            email: user.email
        };

        // Send success response
        res.status(200).json({
            message: "User registered successfully",
            data: userData
        });
    } catch (error: any) {
        // Handle errors
        res.status(500).json({
            message: error.message,
        });
    }
};

/**
 * Creates and sends a JWT token to the client.
 * @param user - User identifier (e.g., email or username).
 * @param res - Express response object.
 * @returns The generated JWT token.
 */
const createSendToken = async (user: string, res: Response) => {
    // Create a JWT token with a 1-day expiration
    const token = Jwt.sign({ id: user }, jwtSecret, {
        expiresIn: "1d"
    });

    // Secure cookie in production
    if (config.env == "production") cookieOptions.secure = true;

    // Set the JWT token in a cookie
    res.cookie("jwt", token, cookieOptions);

    return token;
};

/**
 * Extracts and verifies the user from a JWT token.
 * @param token - JWT token.
 * @returns The decoded user data.
 */
const UserFromToken = (token: any) => {
    try {
        if (!token) {
            throw new ApiError(401, "No token provided");
        }

        // Verify the token and return the user data
        let user = Jwt.verify(token, jwtSecret);
        return user;
    } catch (error) {
        throw new ApiError(401, "Invalid token");
    }
};

/**
 * Handles user login with email and password.
 * @param req - Express request object containing login credentials.
 * @param res - Express response object.
 */
const login = async (req: Request, res: Response) => {
    try {
        // Extract username and password from the request
        const { username, password } = req.body;

        if (!username || !password) {
            res.status(400).json({ message: 'Username and password are required' });
        }

        // Find the user in the database
        const user = await User.findOne({ email: username }).select({ _id: 0, password: 1, email: 1 });

        // Check if the user exists and the password matches
        if (!user || !(await isPasswordMatch(password, user.password as string))) {
            throw new ApiError(400, "Incorrect email or password");
        }

        // Create and send a JWT token
        const token = await createSendToken(user.email, res);

        // Send success response
        res.status(200).json({
            message: "User logged in successfully!",
            token: token
        });
    } catch (error: any) {
        // Handle errors
        if (error instanceof ApiError) {
            res.status(error.statusCode).json({
                message: error.message
            });
        } else {
            res.status(500).json({
                message: error.message
            });
        }
    }
};

/**
 * Tests LDAP authentication.
 * @param kennung - LDAP username.
 * @param password - LDAP password.
 * @returns A promise that resolves to `true` if authentication is successful, otherwise `false`.
 */
const sldapTest = async (kennung: string, password: string): Promise<boolean> => {
    if (!kennung || !password) return false;

    // Prepare LDAP options
    const ldaprdn = `uid=${kennung},${LDAP_OPTS.server.searchBase}`;
    let options = {
        ldapOpts: {
            url: LDAP_OPTS.server.url,
        },
        userDn: ldaprdn,
        userPassword: password,
        userSearchBase: LDAP_OPTS.server.searchBase,
        usernameAttribute: 'uid',
        username: kennung,
    };

    // Attempt LDAP authentication
    let user = await authenticate(options);
    console.log(user);

    return new Promise((resolve, reject) => {
        if (!user) {
            console.error('Failed to bind:');
            resolve(false);
        } else {
            console.log('LDAP bind successful');
            resolve(true);
        }
    });
};

/**
 * Handles user login with LDAP authentication.
 * @param req - Express request object containing LDAP credentials.
 * @param res - Express response object.
 */
const loginLDAP = async (req: Request, res: Response) => {
    try {
        // Extract username and password from the request
        const { username, password } = req.body;

        if (!username || !password) {
            res.status(400).json({ message: 'Username and password are required' });
        }

        // Attempt LDAP authentication
        const isAuthenticated = await sldapTest(username, password);

        if (isAuthenticated) {
            console.log('Authentication successful!');

            // Check if the user is already registered in the database
            const isUserRegistered = await LDapUser.exists({ username: username });
            if (!isUserRegistered) {
                // Register the user in the database
                await LDapUser.create({
                    username: username,
                    password: await encryptPassword(password)
                });
            }

            // Create and send a JWT token
            const token = await createSendToken(username, res);

            // Send success response
            res.status(200).json({ message: 'Authenticated via LDAP', token, user: username });
        } else {
            // Send error response for invalid credentials
            res.status(401).json({ message: 'Invalid username or password' });
        }
    } catch (err: any) {
        // Handle errors
        console.error('LDAP error:', err);
        res.status(500).json({ message: err.message });
    }
};

// Export the functions for use in other parts of the application
export default { register, login, loginLDAP, UserFromToken };