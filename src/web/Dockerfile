# Web — Angular build served by unprivileged nginx. Context: src/web
#   docker build -t smartfactory/web src/web
#   docker build --build-arg BUILD_CONFIGURATION=compose -t smartfactory/web src/web   (what Compose does)
FROM node:22-alpine AS build
# Why a build argument: Angular bakes the API and Keycloak URLs in at build time (src/environments).
ARG BUILD_CONFIGURATION=production
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY . .
RUN npm run build -- --configuration "$BUILD_CONFIGURATION"

FROM nginxinc/nginx-unprivileged:1.31-alpine AS runtime
COPY nginx/default.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/web/browser /usr/share/nginx/html
EXPOSE 8080
