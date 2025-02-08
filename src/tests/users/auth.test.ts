import {request} from '../helper'

describe('Correct Login Request', ()=>{
    it('should a successful authentication',async ()=>{

        const resp:any = await request.post('/login')
                                    .set('Accept', 'application/json')
                                   .send({username:'john.doe@test.com', password:'123456789'})                
                                   .expect(200)
        
        expect(resp.body.message).toEqual('User logged in successfully!')

    })
})


describe('Login Request', ()=>{
    

    it('should return an error while username is empty',async ()=>{

        const resp:any = await request.post('/login')
                                   .send({username:'', password:'test'})
                                   .set('Accept', 'application/json')
                                   .expect(400)
       
        expect(resp.body.message).toEqual('Username and password are required')

    })

   

})

