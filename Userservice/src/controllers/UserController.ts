import express, { Request, Response } from "express";
import { User } from "../database";
import {ApiError} from "../utils";
import AuthController from "./AuthController"
import { Jwt, JwtPayload } from "jsonwebtoken";



const saveGraphics = async(req: Request, res: Response) =>{
    try {
        let  {email, graphics}  = req.body;       
        const user = await User.findOne({ email });
      
        if (!user) {
          res.status(404).json({
            message: "User not found",
          });
          return;
        }
    
        const existingGraphic = user.graphics.find((g) => g.class === graphics.class);
        
        if (existingGraphic) {
          Object.assign(existingGraphic, graphics);
        } else {
          
          user.graphics.push(graphics);
        }
    
        await user.save();
    
        res.status(200).json({
          message: "Graphic saved",
          data: user,
        });
      } catch (error: any) {
        res.status(500).json({
          message: error.message,
        });
      }
}

const getGraphics = async(req: Request, res: Response) =>{
    try {
       console.log(req.body)
       let {email} = req.body
        /*let token = req.headers.authorization
        console.log('token: '+ token)
        let user = AuthController.UserFromToken(token) as JwtPayload*/
        //console.log(graphics)
        const userExists = await User.findOne({email:email});
        
        if (userExists)
        {
            //console.log(userExists.graphics)
            res.status(200).json({
                message:"Graphic saved",
                data: userExists.graphics
            })
        }
        else{
            res.status(500).json({
                message:"Fetch graphic failed",
            })
        }
    
    } catch (error: any) {
         res.status(500).json({
            message: error.message,
        });
    }
}




export default { saveGraphics, getGraphics};

