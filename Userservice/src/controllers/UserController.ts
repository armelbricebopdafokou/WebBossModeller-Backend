import express, { Request, Response } from "express";
import { LDapUser, User } from "../database";
import {ApiError} from "../utils";
import AuthController from "./AuthController"
import { Jwt, JwtPayload } from "jsonwebtoken";



const saveGraphics = async(req: Request, res: Response) =>{
    try {
      
          let user:any
          if(req.body.email)
          {
            user = await savegraphicForUser(req, res)
          }
          else{
            user = savegraphicForLDAPUser(req, res)
          }
          
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


const savegraphicForUser = async(req: Request, res: Response)=>{
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
  return user
}

const savegraphicForLDAPUser = async(req: Request, res: Response)=>{
  let  {username, graphics}  = req.body;       
  const user = await LDapUser.findOne({ username });

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
  return user
}


const getGraphics = async(req: Request, res: Response) =>{
    try {

      let userExists:any
       if(req.body.email)
       {
          userExists = await loadgraphicForUser(req)
       }else{
        userExists = await loadgraphicForLDAPUser(req)
       }
      
        if (userExists)
        {
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


const loadgraphicForUser = async(req: Request)=>{
    let {email} = req.body
        /*let token = req.headers.authorization
        console.log('token: '+ token)
        let user = AuthController.UserFromToken(token) as JwtPayload*/
        //console.log(graphics)
    const userExists = await User.findOne({email:email});
    return userExists
}

const loadgraphicForLDAPUser = async(req: Request)=>{
  let {username} = req.body
        
    const userExists = await LDapUser.findOne({username:username});
    return userExists
}


export default { saveGraphics, getGraphics};

