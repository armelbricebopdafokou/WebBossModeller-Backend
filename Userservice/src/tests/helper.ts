import {agent as _request} from 'supertest'
import app from '../server'


export const request = _request(app)