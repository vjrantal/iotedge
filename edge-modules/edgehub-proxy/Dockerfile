FROM haproxy:1.8.14-alpine

RUN apk add --no-cache curl jq

RUN mkdir -p /run/haproxy

COPY haproxy.cfg /usr/local/etc/haproxy/haproxy.cfg

COPY iotedge.pem /certs/iotedge.pem

EXPOSE 8899

COPY docker-entrypoint.sh /
COPY target/x86_64-unknown-linux-musl/release/edgehub-proxy /usr/bin
