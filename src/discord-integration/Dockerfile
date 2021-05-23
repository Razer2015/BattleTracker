# Use Node.js v16
FROM node:16-alpine3.13

WORKDIR /usr/src/app

# Copy over the whole folder content
COPY . ./

# Make a clean npm install and only install modules
# needed for production
RUN npm ci --only=production

# Run the service on container startup.
CMD [ "npm", "run", "start" ]