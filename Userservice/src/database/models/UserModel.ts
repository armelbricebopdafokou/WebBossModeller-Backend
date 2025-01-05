import mongoose, { Schema, Document} from "mongoose"
import validator, { trim } from "validator";

export interface IUser extends Document{
    lastName:string;
    firstName:string;
    email:string;
    password:string;
    graphics: [any]
    createdAt:Date;
    updatedAt:Date;
}

const UserSchema:Schema = new Schema(
    {
        lastName:{
            type: String,
            trim:true,
            require: [true, "Name must be provided"],
            minlength: 3
        },
        firstName:{
            type: String,
            trim:true,
            minlength: 3
        },
        email:{
            type: String,
            required: true,
            unique: true,
            trim: true,
            validate:[validator.isEmail, "Please provide a valid email."]
        },
        password:{
            type: String,
            trim:false,
            require: [true, "Password must be provided"],
            minlength: 8
        },
        graphics:{
            type:Array,
            require: false
        }
    },
    {
        timestamps:true
    }
);

const User = mongoose.model<IUser>("User", UserSchema);
export default User;