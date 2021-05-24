# This stage installs our modules
FROM mhart/alpine-node:16
WORKDIR /app
COPY package.json package-lock.json ./

RUN npm ci --prod

# Then we copy over the modules from above onto a `slim` image
FROM mhart/alpine-node:slim-16

WORKDIR /app
COPY --from=0 /app .
COPY . .

CMD ["node", "src/index.js"]