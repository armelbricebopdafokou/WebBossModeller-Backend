FROM node:20-alpine

WORKDIR /gateway/src/app

COPY package.json ./

RUN npm install

COPY . .

RUN npm run build

EXPOSE 8082

CMD ["npm", "start"]
