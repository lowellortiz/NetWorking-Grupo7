#!/usr/bin/env bash
set -euo pipefail

minikube_version="v1.39.0"
kubernetes_version="v1.34.6"
agones_version="1.60.0"

architecture="$(dpkg --print-architecture)"
if [[ "$architecture" != "amd64" && "$architecture" != "arm64" ]]; then
  echo "Unsupported Debian architecture: $architecture" >&2
  exit 1
fi

curl -LO "https://github.com/kubernetes/minikube/releases/download/${minikube_version}/minikube-linux-${architecture}"
sudo install "minikube-linux-${architecture}" /usr/local/bin/minikube
rm "minikube-linux-${architecture}"

minikube start -p agones --driver=docker --kubernetes-version="${kubernetes_version}" --cpus=4 --memory=6144
minikube kubectl -p agones -- create namespace agones-system
minikube kubectl -p agones -- apply --server-side -f "https://raw.githubusercontent.com/agones-dev/agones/release-${agones_version}/install/yaml/install.yaml"
minikube kubectl -p agones -- wait --for=condition=available deployment/agones-controller -n agones-system --timeout=300s

echo "Minikube and Agones ${agones_version} are installed."
echo "Use: minikube kubectl -p agones -- get pods -n agones-system"
