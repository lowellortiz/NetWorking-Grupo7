#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$project_root"

if [[ "$(uname -m)" != "x86_64" ]]; then
  echo "ERROR: los Dedicated Servers actuales son Linux x86_64; este script requiere Debian amd64." >&2
  exit 1
fi

docker build -f Infrastructure/Containers/NGO/Dockerfile -t networking-lab-ngo:local .
docker build -f Infrastructure/Containers/NFE/Dockerfile -t networking-lab-nfe:local .
minikube image load -p agones networking-lab-ngo:local
minikube image load -p agones networking-lab-nfe:local

echo "Images built and loaded into minikube profile 'agones'."
