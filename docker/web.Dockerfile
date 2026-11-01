FROM node:22-bookworm AS build
WORKDIR /src

COPY client/package.json client/package-lock.json ./
RUN npm ci

COPY client/ ./
RUN npm run build

FROM nginx:1.28-alpine
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist/vehicle-explorer-ui/browser /usr/share/nginx/html
EXPOSE 80
