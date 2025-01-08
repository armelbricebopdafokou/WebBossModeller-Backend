import { Router } from "express";
import UserController from "../controllers/UserController";
import {authMiddleware} from "../middleware/index"

const userRouter = Router();

userRouter.post("/graphics", authMiddleware, UserController.saveGraphics);
userRouter.get("/graphics", authMiddleware, UserController.getGraphics);
//userRouter.post("/graphics", UserController.saveGraphics)
//userRouter.post("/graphics", UserController.saveGraphics);
export default userRouter;