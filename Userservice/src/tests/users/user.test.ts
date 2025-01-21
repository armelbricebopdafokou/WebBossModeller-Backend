import {request} from '../helper'


describe('Grafik', ()=>{
    

    it('should return an error because token is missing',async ()=>{

        const resp:any = await request.get('/graphics')
                                      .expect(401)

        
        expect(resp.body.message).toEqual('Missing authorization header')

    })

   

})