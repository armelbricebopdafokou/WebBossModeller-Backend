import express, { Request, Response } from "express";
import Jwt from "jsonwebtoken";
import { User } from "../database";
import config from "../config/config";
import { IUser } from "../database";
import ldap from 'ldapjs';
import {ApiError, encryptPassword, isPasswordMatch} from "../utils";
import {authenticate} from 'ldap-authentication'

const jwtSecret = config.JWT_SECRET as string;
const COOKIE_EXPIRATION_DAYS = 90 // cookie expiration in days
const expirationDate = new Date(
    Date.now() + COOKIE_EXPIRATION_DAYS * 24 * 60 * 60 * 1000
);

const cookieOptions = {
    expires: expirationDate,
    secure: false,
    httpOnly: true
};

// LDAP Configuration
const LDAP_OPTS = {
    server: {
        url: 'ldaps://ods0.hs-bochum.de:636',
        searchBase: 'ou=People,o=hs-bochum.de,o=isp',
    }
  };


const register = async (req: Request, res: Response) =>{
    try {
        const { lastName, firstName, email, password} = req.body;
        const userExists = await User.findOne({email});
        if(userExists)
        {
            throw new ApiError(400, "User already exists!")
        }
        const user = await User.create({
            lastName,
            firstName,
            email,
            password: await encryptPassword(password)
        });

        const userData= {
            id: user._id,
            lastName: user.lastName,
            firstName: user.firstName,
            email: user.email
        }

         res.status(200).json({
            message: "User registered successfully",
            data: userData
        })
    } catch (error: any) {
         res.status(500).json({
            message: error.message,
        })
    }
};

const createSendToken = async (user:IUser, res: Response)=>{
    const {lastName, firstName, email } = user;
    const token = Jwt.sign({lastName,firstName, email}, jwtSecret, {
        expiresIn: "1d"
    });
    if(config.env == "production") cookieOptions.secure = true;

    res.cookie("jwt", token, cookieOptions);

    return token;
};

const UserFromToken =  (token:any)=>{
    try {
        if(!token){
            throw new ApiError(401, "No token provided")
        }
        let user = Jwt.verify(token, jwtSecret);
        return user;    
    }catch(error){
        throw new ApiError(401, "Invalid token")
    }
     
};

const login = async(req: Request, res: Response) =>{
    
    try {
        
       // Extract username and password from the request
        const { username, password } = req.body;
        
        if (!username || !password) {
             res.status(400).json({ message: 'Username and password are required' });
          }

        const user = await User.findOne({email:username}).select("+password");

        if(!user || 
            !(await isPasswordMatch(password, user.password as string))
        ){
            throw new ApiError(400, "Incorrect email or password");
        }

        const token = await createSendToken(user!, res);

            res.status(200).json({
            message:"User logged in successfully!",
            token: token
        })
        
    } catch (error: any) {
        if (error instanceof ApiError)
        {
            res.status(error.statusCode).json({
                message: error.message
            });
        }else{
            res.status(500).json({
                message: error.message
            });
        }
         
    }

};

const sldapTest = async (kennung: string, password: string): Promise<boolean> => {
    if (!kennung || !password) return false;

    const ldaprdn = `uid=${kennung},${LDAP_OPTS.server.searchBase}`;
    const client = ldap.createClient({
        url: LDAP_OPTS.server.url,
    });

    return new Promise((resolve, reject) => {
        client.bind(ldaprdn, password, (err: any) => {
            if (err) {
                console.error('Failed to bind:', err.message);
                client.unbind();
                resolve(false);
            } else {
                console.log('LDAP bind successful');
                client.unbind();
                resolve(true);
            }
        });
    });
};

const loginLDAP = async(req: Request, res: Response) =>{
    
    try {
        
       // Extract username and password from the request
        const { username, password } = req.body;
      
        if (!username || !password) {
             res.status(400).json({ message: 'Username and password are required' });
          }
          // Try LDAP Authentication
          const isAuthenticated = await sldapTest(username, password);

          if (isAuthenticated) {
              console.log('Authentication successful!');
              const isUserRegistered = await User.findOne({ email:username });
              if(!isUserRegistered)
              {
                await User.create({
                    email:username,
                    password: await encryptPassword(password)
                  });
              }
             
              // Issue JWT Token or perform other actions upon successful authentication
              const token = await createSendToken(username, res);
               res.status(200).json({ message: 'Authenticated via LDAP', token, user: username });
          } else {
               res.status(401).json({ message: 'Invalid username or password' });
          }
      } catch (err) {
          console.error('LDAP error:', err);
           res.status(500).json({ message: 'LDAP error', error: err });
      }
    };

export default { register, login, loginLDAP, UserFromToken};
