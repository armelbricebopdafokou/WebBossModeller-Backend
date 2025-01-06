import express, { Request, Response } from "express";
import { User } from "../database";
import {ApiError} from "../utils";



const saveGraphics = async(req: Request, res: Response) =>{
    try {
       
        let {email, graphics} = req.body
        //console.log(graphics)
        const userExists = await User.updateOne({email:email}, {$push:{graphics:graphics}});
        console.log(userExists)
        if (userExists)
        {
            res.json({
                status:200,
                message:"Graphic saved",
                data: userExists
            })
        }
        else{
            res.json({
                status:500,
                message:"Update failed",
            })
        }
    
    } catch (error: any) {
         res.json({
            status: 500,
            message: error.message,
        });
    }
}

export default { saveGraphics};