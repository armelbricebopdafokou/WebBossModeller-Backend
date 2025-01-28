import mongoose, { Schema, Document} from "mongoose"
import validator, { trim } from "validator";

export interface ILDAPUser extends Document{
    username:string;
    password:string;
    graphics: [any]
    createdAt:Date;
    updatedAt:Date;
}

const LDAPUserSchema:Schema = new Schema(
    {
        username:{
            type: String,
            trim:true,
            require: [true, "Name must be provided"],
            minlength: 3
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

const LDapUser = mongoose.model<ILDAPUser>("LDapUser", LDAPUserSchema);
export default LDapUser;