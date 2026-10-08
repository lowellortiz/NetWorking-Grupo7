#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$project_root"

if [[ "$(uname -m)" != "x86_64" ]]; then
  echo "ERROR: el build NFE de este laboratorio requiere Debian amd64." >&2
  exit 1
fi

if [[ ! -f Builds/NFE-LinuxServer/NetworkingLabNFEServer.x86_64 ]]; then
  echo "ERROR: falta Builds/NFE-LinuxServer/NetworkingLabNFEServer.x86_64." >&2
  echo "Genera el build Linux Server en Unity y transfiere toda su carpeta." >&2
  exit 1
fi

docker build -f Infrastructure/Containers/NFE/Dockerfile -t networking-lab-nfe:local .
minikube image load -p agones networking-lab-nfe:local
echo "Imagen NFE construida y cargada en el perfil agones."
