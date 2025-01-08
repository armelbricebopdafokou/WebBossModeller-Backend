import mongoose, { Schema, Document} from "mongoose"
import validator, { trim } from "validator";

export interface IGraphic extends Document{
    name:string;
    data:any;
    createdAt:Date;
    updatedAt:Date;
}

const GraphicSchema:Schema = new Schema(
    {
        name:{
            type: String,
            trim:true,
            require: [true, "Name must be provided"],
            minlength: 3
        },
        data:{
            type: Object,
        },

    },
    {
        timestamps:true
    }
);

const Graphic = mongoose.model<IGraphic>("Graphic", GraphicSchema);
export default Graphic;