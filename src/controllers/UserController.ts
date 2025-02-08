import express, { Request, Response } from "express";
import { User, LDapUser } from "../database"; // Ensure these are imported correctly
import { ApiError } from "../utils";
import { Model, Document } from "mongoose";

// Define a type for Mongoose models
type MongooseModel<T extends Document> = Model<T>;

// Helper function to save graphics
const saveGraphic = async (
    req: Request,
    res: Response,
    userModel: MongooseModel<any>,
    identifier: string
) => {
    const { graphics } = req.body;
    const user = await userModel.findOne({ [identifier]: req.body[identifier] });

    if (!user) {
        throw new ApiError(404, "User not found");
    }

    const existingGraphic = user.graphics.find((g:any) => g.class === graphics.class);
    if (existingGraphic) {
        Object.assign(existingGraphic, graphics);
    } else {
        user.graphics.push(graphics);
    }

    await user.save();
    return user;
};

// Helper function to load graphics
const loadGraphics = async (
    req: Request,
    userModel: MongooseModel<any>,
    identifier: string
) => {
    const user = await userModel.findOne({ [identifier]: req.body[identifier] });
    if (!user) {
        throw new ApiError(404, "User not found");
    }
    return user.graphics;
};

// Route handler to save graphics
const saveGraphics = async (req: Request, res: Response) => {
    try {
        const user = req.body.email
            ? await saveGraphic(req, res, User, "email")
            : await saveGraphic(req, res, LDapUser, "username");

        res.status(200).json({
            message: "Graphic saved successfully",
            data: user,
        });
    } catch (error: any) {
        res.status(error.statusCode || 500).json({
            message: error.message,
        });
    }
};

// Route handler to get graphics
const getGraphics = async (req: Request, res: Response) => {
    try {
        const graphics = req.body.email
            ? await loadGraphics(req, User, "email")
            : await loadGraphics(req, LDapUser, "username");

        res.status(200).json({
            message: "Graphics fetched successfully",
            data: graphics,
        });
    } catch (error: any) {
        res.status(error.statusCode || 500).json({
            message: error.message,
        });
    }
};

export default { saveGraphics, getGraphics };