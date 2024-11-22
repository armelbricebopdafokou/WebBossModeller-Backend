import express, { Request, Response } from "express";
import Jwt from "jsonwebtoken";
import { User } from "../database";
import config from "../config/config";
import { IUser } from "../database";
import {ApiError, encryptPassword, isPasswordMatch} from "../utils";

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

         res.json({
            status: 200,
            message: "User registered successfully",
            data: userData
        })
    } catch (error: any) {
         res.json({
            status: 500,
            message: error.message,
        })
    }
};

const createSendToken = async (user:IUser, res: Response)=>{
    const {lastName, firstName, email, id } = user;
    const token = Jwt.sign({lastName, email, id}, jwtSecret, {
        expiresIn: "1d"
    });
    if(config.env == "production") cookieOptions.secure = true;

    res.cookie("jwt", token, cookieOptions);

    return token;
};

const login = async(req: Request, res: Response) =>{
    
    try {
       
        const  {email, password} = req.body;
        const user = await User.findOne({email}).select("+password");

        if(!user || 
            !(await isPasswordMatch(password, user.password as string))
        ){
            throw new ApiError(400, "Incorrect email or password");
        }

        const token = await createSendToken(user!, res);

         res.json({
            status: 200,
            message:"User logged in successfully!",
            token
        })

    } catch (error: any) {
         res.json({
            status: 500,
            message: error.message,
        });
    }

};



export default { register, login};