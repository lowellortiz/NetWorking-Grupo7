# Infraestructura local del laboratorio

Esta carpeta contiene los archivos reproducibles para ejecutar los dos Dedicated Servers mediante Minikube y Agones dentro de Debian.

## Compatibilidad verificada del artefacto

Los builds generados por `BuildTarget.StandaloneLinux64` son ejecutables ELF `x86-64`. Por tanto, las imágenes y los manifiestos de esta versión son **linux/amd64**. En Debian `arm64` (la VM nativa de VirtualBox sobre Apple Silicon), Kubernetes y Agones pueden instalarse, pero estos binarios Unity no pueden ejecutarse de forma nativa. No se declara soporte multi-arquitectura.

## Puertos

- NGO: UDP 7979.
- NFE: UDP 7980.

La integración de ciclo de vida de Agones (`Ready` y `Health`) es una actividad del estudiante descrita en la guía; no está implementada en el starter.

## Flujo amd64

1. Generar ambos builds Linux Server desde Unity.
2. Ejecutar `Infrastructure/Scripts/install-minikube-agones.sh` dentro de Debian amd64.
3. Ejecutar `Infrastructure/Scripts/build-images.sh` desde una copia del proyecto que incluya `Builds/`.
4. Aplicar uno de los manifiestos de `Infrastructure/Kubernetes/Agones/`.
5. Consultar `status.address` y `status.ports[0].port` con `kubectl get gameserver`.
