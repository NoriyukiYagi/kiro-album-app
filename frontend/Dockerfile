FROM node:18-alpine AS build

WORKDIR /app

# Install Angular CLI
RUN npm install -g @angular/cli@17

# Copy package files
COPY package*.json ./
RUN npm install

# Copy source code and build
COPY . ./
RUN ng build --configuration production

FROM nginx:alpine AS runtime

# Copy built app to nginx
COPY --from=build /app/dist/album-app /usr/share/nginx/html

# Copy nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Expose port
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]