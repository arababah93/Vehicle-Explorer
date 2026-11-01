#!/bin/bash
set -euxo pipefail

dnf update -y
dnf install -y docker git

if ! swapon --show | grep -q /swapfile; then
  fallocate -l 2G /swapfile || dd if=/dev/zero of=/swapfile bs=1M count=2048
  chmod 600 /swapfile
  mkswap /swapfile
  swapon /swapfile
fi

systemctl enable --now docker

install -d /usr/libexec/docker/cli-plugins
curl -fsSL "https://github.com/docker/compose/releases/download/v2.40.3/docker-compose-linux-x86_64" \
  -o /usr/libexec/docker/cli-plugins/docker-compose
curl -fsSL "https://github.com/docker/buildx/releases/download/v0.38.1/buildx-v0.38.1.linux-amd64" \
  -o /usr/libexec/docker/cli-plugins/docker-buildx
chmod +x /usr/libexec/docker/cli-plugins/docker-compose /usr/libexec/docker/cli-plugins/docker-buildx

install -d /opt/vehicle-explorer
git clone --depth 1 https://github.com/arababah93/Vehicle-Explorer.git /opt/vehicle-explorer
cd /opt/vehicle-explorer

docker compose build api
docker compose build web
WEB_PORT=80 docker compose up -d
