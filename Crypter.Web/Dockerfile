FROM caddy:2.6-alpine AS webhost
COPY Caddyfile /etc/caddy/Caddyfile
COPY bin/Release/net7.0/publish/wwwroot/ /srv/
EXPOSE 8080
