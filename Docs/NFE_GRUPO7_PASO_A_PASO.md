# NFE - Grupo 7: implementación y pruebas

Proyecto: NetWorking-Grupo7, rama de trabajo `main`.
Unity: **6000.6.0f1**. Equipo de desarrollo: Windows x86-64.
Servidor final del laboratorio: Debian amd64 en VirtualBox, con Docker, Minikube y Agones.

## Qué está preparado y qué falta comprobar

Esta versión incorpora el código de NFE-01, NFE-02, NFE-03 y NFE-04. También repara las referencias de authoring del prefab y de la SubScene, añade menús para generar los builds NFE y añade Ready/Health de Agones.

El código se revisó fuera de Unity. **La compilación en Unity, los builds, la VM y las pruebas multijugador no se han ejecutado aquí.** No presentes esos checkpoints como aprobados antes de probarlos en tu computador. Los tres TODO de NGO conservan su código original.

## 1. Incorporar los cambios en el repositorio

Si vas a abrir este ZIP como proyecto independiente, descomprímelo y añade su carpeta desde Unity Hub. La carpeta correcta contiene `Assets`, `Packages` y `ProjectSettings`.

Si ya tienes el repositorio clonado, primero revisa y guarda los cambios locales del equipo. Actualiza `main` antes de incorporar el trabajo. El ZIP incluye un parche, `NFE_Grupo7.patch`, que contiene solamente los cambios de esta entrega. Cópialo temporalmente fuera del repositorio y, desde la raíz de tu clon, ejecuta:

```powershell
git switch main
git pull
git apply --check "C:\ruta\NFE_Grupo7.patch"
git apply "C:\ruta\NFE_Grupo7.patch"
git status --short
```

Reemplaza la ruta del parche por su ubicación real. Si `git apply --check` informa un conflicto, conserva los archivos del equipo y revisa esa diferencia antes de aplicar. El parche se generó respecto al ZIP recibido; una modificación posterior de `main` puede requerir combinar cambios.

Como alternativa, copia manualmente únicamente los archivos indicados en `Docs/NFE_ARCHIVOS_CAMBIADOS.txt`, incluidos sus `.meta`. No reemplaces todo el repositorio si un compañero ya avanzó en otras partes.

Después de compilar y hacer la prueba local:

```powershell
git add Assets/Scripts/NFE Assets/Scripts/Editor/NfeBuildTools.cs Assets/Scripts/Editor/NfeBuildTools.cs.meta Assets/Prefabs/NFEPlayer.prefab Assets/Scenes/NFE/NFEGameplaySubScene.unity Infrastructure/Containers/NFE/Dockerfile Infrastructure/Scripts/build-nfe-image.sh Infrastructure/Scripts/forward-nfe-udp.py Infrastructure/Scripts/start-nfe-clients.ps1 Infrastructure/Scripts/start-nfe-server.ps1 Infrastructure/README.md README.md Docs/NFE_GRUPO7_PASO_A_PASO.md Docs/NFE_ARCHIVOS_CAMBIADOS.txt
git diff --cached --stat
git commit -m "Completar NFE y preparar servidor dedicado"
git push origin main
```

Comprueba el diff antes de confirmar para que el commit incluya los cambios que acabas de probar. Los builds y los logs quedan ignorados por Git.

## 2. Abrir Unity y revisar la escena

1. Abre el proyecto con Unity **6000.6.0f1**, la versión de `ProjectSettings/ProjectVersion.txt`.
2. Deja que Unity resuelva los paquetes y termine de importar. Conserva `Packages/manifest.json` y `packages-lock.json`.
3. Abre `Assets/Scenes/NFEGameplay.unity`.
4. Abre la consola y resuelve el primer error rojo si aparece. Guarda el mensaje completo y el archivo/línea.
5. Revisa `Assets/Prefabs/NFEPlayer.prefab`: debe tener `NfePlayerAuthoring`, `GhostAuthoringComponent`, Has Owner y Support Auto Command Target activados, y modo Owner Predicted.
6. Abre la SubScene `Assets/Scenes/NFE/NFEGameplaySubScene.unity`. Su objeto `NFE Player Spawner` debe tener `NfePlayerSpawnerAuthoring` y el prefab `NFEPlayer` asignado.
7. En la escena principal, confirma que la SubScene tiene Auto Load Scene activado.

Los dos scripts de authoring ahora están en archivos separados con referencias GUID reales. El starter recibido tenía una referencia de jugador vacía y un script de spawner incrustado en la escena. No hace falta ejecutar `Build NGO and NFE Scenes` para esta entrega: ese menú reconstruye también los recursos de NGO.

Si el Editor indica que no existe ServerWorld, revisa el **PlayMode Tools de NetCode** y selecciona ClientAndServer para la prueba Host. El bootstrap respeta esta configuración. El escenario de **Multiplayer Play Mode** que abre varios procesos es otra herramienta; no debe confundirse con la selección de Worlds.

## 3. Primera prueba: Host y un cliente

1. En `NFEGameplay`, presiona Play.
2. Usa Address `127.0.0.1` y Port `7980`.
3. Pulsa **Start Host** una sola vez.
4. Debe aparecer `Local client ID: ...` y una cápsula con nombre y color.
5. Enfoca la vista Game. Usa **WASD** para mover tu jugador y **Espacio** para PowerUp. Evita dejar enfocado el campo de dirección al jugar.
6. Detén Play, genera el cliente Windows desde `Networking Lab > NFE > Build Windows Client` y vuelve a iniciar Host en el Editor.
7. Desde PowerShell, situado en la raíz del proyecto, inicia un cliente adicional:

```powershell
powershell.exe -File .\Infrastructure\Scripts\start-nfe-clients.ps1 -Address 127.0.0.1 -Port 7980 -Clients 1
```

Si Windows muestra que no permite ejecutar scripts, puedes ejecutar el cliente directamente:

```powershell
& ".\Builds\NFE-WindowsClient\NetworkingLabNFEClient.exe" -nfe -client -address 127.0.0.1 -port 7980
```

No es necesario modificar la política permanente de PowerShell. Si Windows solicita permitir el ejecutable en la red, autoriza la red privada que usarás para el laboratorio.

Cada proceso cliente usa WASD y Espacio para **su propio jugador**. Los controles diferentes de cuatro jugadores en un teclado pertenecen a `LocalGameplayBaseline`, no a la prueba de red NFE.

Comprueba que hay dos identidades distintas, cada ventana controla su cápsula, los movimientos se ven en ambas y PowerUp muestra el mismo jugador en ambas ventanas durante unos dos segundos. Cierra el cliente adicional antes de seguir.

## 4. Qué resuelve cada actividad

| Actividad | Código principal | Comprobación |
|---|---|---|
| NFE-01 | `NfeConnectionUI`: consulta RW de `NetworkStreamDriver`, Listen y Connect | El servidor escucha UDP 7980 y el cliente obtiene NetworkId. |
| NFE-02 | `NfeGoInGameClientSystem` y `NfeGoInGameServerSystem` | Un Ghost por conexión, GhostOwner correcto y posiciones iniciales separadas. |
| NFE-03 | `NfeMovementSystem` en PredictedSimulationSystemGroup | Owner predice, servidor simula, los snapshots actualizan a los demás y se respetan los límites. |
| NFE-04 | `NfePowerUpServerSystem` y `NfePowerUpClientSystem` | InputEvent se procesa en el servidor; RPC fiable llega a cada UI y su entidad se consume. |

NFE-02 descarta solicitudes repetidas y conexiones cerradas. El número del jugador viene del NetworkId asignado por el servidor. `LinkedEntityGroup` vincula el Ghost con su conexión para que NetCode lo destruya al desconectar.

El movimiento tiene velocidad 5 y límites X entre -10.5 y 10.5, Z entre -7.5 y 7.5. La dirección se limita para que la diagonal no sea más rápida; el servidor descarta valores no finitos.

Dos detalles de los ejemplos de la guía se adaptaron a Netcode 6.6:

- Desde un MonoBehaviour, el singleton se obtiene con `EntityQuery.GetSingletonRW<NetworkStreamDriver>()`; no se llama a `EntityManager.GetComponentDataRW` sin indicar una entidad.
- `SendRpcCommandRequest.TargetConnection = Entity.Null` realiza el broadcast. La API documentada de 6.6 no contiene `BroadcastTargets` ni requiere `RpcBroadcastTargets.All`.

## 5. Generar los builds

Los nuevos menús generan solo `NFEGameplay`, sin mezclar baseline o NGO. La SubScene se incluye como dependencia de la escena principal.

| Menú `Networking Lab > NFE` | Resultado |
|---|---|
| Build Windows Client | `Builds/NFE-WindowsClient/NetworkingLabNFEClient.exe` |
| Build Windows Server | `Builds/NFE-WindowsServer/NetworkingLabNFEServer.exe` |
| Build Linux Server | `Builds/NFE-LinuxServer/NetworkingLabNFEServer.x86_64` |

Instala desde Unity Hub los módulos que el Editor solicite para Windows Dedicated Server y Linux Dedicated Server. Para el Linux Server desde Windows necesitas el soporte de compilación Linux correspondiente. Si el build informa que no está instalado, agrega ese módulo a la instalación **6000.6.0f1** y repite el menú.

Detén Play antes de generar un build. Espera `NFE_BUILD: ...` en la consola. Un archivo ejecutable solo no es suficiente: conserva y transfiere **toda** la carpeta del build, incluida su carpeta de datos y bibliotecas.

El bootstrap crea solo ServerWorld en Dedicated Server y solo ClientWorld cuando se inicia con `-client`. Los menús de servidor permiten HTTP para el REST SDK local de Agones al compilar y restauran después la opción del proyecto. Si generas el servidor manualmente con Build Profiles, revisa esa opción de HTTP para que las llamadas locales al sidecar funcionen.

## 6. Cuatro clientes en Windows antes de Linux

Para una prueba reproducible puedes usar el servidor Windows preparado, con el Editor fuera de Play. Genera Windows Server y Windows Client; después ejecuta:

```powershell
powershell.exe -File .\Infrastructure\Scripts\start-nfe-server.ps1 -Port 7980
powershell.exe -File .\Infrastructure\Scripts\start-nfe-clients.ps1 -Address 127.0.0.1 -Port 7980 -Clients 4
```

El primer script muestra el PID del servidor y la ruta del log. El segundo abre cuatro procesos cliente. Si prefieres usar el Editor como servidor, presiona Play y **Start Server**, sin pulsar Start Host; luego abre los cuatro clientes con el segundo script.

Espera conexión de cada ventana. Mueve cada jugador por separado y activa PowerUp en cada una. Deben existir cuatro jugadores distintos. Mantén una dirección contra un borde; no debe atravesarlo. Cierra una ventana y comprueba que su jugador desaparece en las otras; al volver a conectar debe crearse un solo jugador.

En `Window > Entities > Hierarchy`, inspecciona ClientWorld y ServerWorld. El Ghost tiene `GhostOwner.NetworkId` igual al NetworkId de su conexión. Solo el Ghost propietario lleva `GhostOwnerIsLocal` habilitado en ese cliente.

Para detener el servidor Windows, usa el PID concreto que imprimió el script:

```powershell
Stop-Process -Id <PID_DEL_SERVIDOR>
```

Reemplaza el marcador por el PID observado. Cierra los cuatro clientes. No mantengas dos servidores intentando escuchar UDP 7980 en el mismo Windows.

## 7. Debian y servidor Linux directo

Sigue las secciones 9 a 15 de la guía del docente: VirtualBox en Windows, Debian **amd64**, adaptador NAT, adaptador Host-Only y SSH. En Debian observa:

```bash
uname -m
ip -br addr
```

La arquitectura debe ser `x86_64`. Identifica la IP del adaptador Host-Only; esa es la dirección que usará Windows. `127.0.0.1` en Windows solo sirve para un servidor que corre en el mismo Windows.

Genera Linux Server desde Unity. Desde PowerShell transfiere toda su carpeta:

```powershell
scp -r .\Builds\NFE-LinuxServer usuario@<IP_VM>:/home/usuario/nfe-server
```

Sustituye usuario e IP por los valores de tu VM. En Debian:

```bash
cd ~/nfe-server
chmod +x NetworkingLabNFEServer.x86_64
file NetworkingLabNFEServer.x86_64
./NetworkingLabNFEServer.x86_64 -batchmode -nographics -nfe -port 7980 -logFile -
```

En otra terminal de Debian:

```bash
ss -lunp | rg 7980
```

Si no tienes `rg`, usa `grep 7980`. La salida debe mostrar el socket UDP del proceso. Si el ejecutable informa una biblioteca ausente, consulta `ldd NetworkingLabNFEServer.x86_64` e instala la dependencia identificada antes de continuar.

Desde Windows conecta **un** cliente a la IP Host-Only de Debian. Cuando funcione, abre cuatro:

```powershell
powershell.exe -File .\Infrastructure\Scripts\start-nfe-clients.ps1 -Address <IP_VM> -Port 7980 -Clients 4
```

Guarda las evidencias. Detén este servidor directo con Ctrl+C antes de iniciar el contenedor.

## 8. Contenedor NFE

En Debian necesitas una copia de la raíz del proyecto que incluya `Infrastructure` y `Builds/NFE-LinuxServer`. Desde esa raíz:

```bash
docker build -f Infrastructure/Containers/NFE/Dockerfile -t networking-lab-nfe:local .
docker run --rm --name nfe-direct-test -p 7980:7980/udp networking-lab-nfe:local
```

En otra terminal revisa `docker logs nfe-direct-test` y prueba desde Windows contra la IP Host-Only. Aquí todavía no existe el sidecar de Agones; que no aparezca `NFE_AGONES: Ready accepted` es normal en esta prueba.

Detén el contenedor antes de seguir:

```bash
docker stop nfe-direct-test
```

## 9. Minikube, Agones y GameServer NFE

Prepara Docker y Minikube siguiendo la guía. El starter incluye `install-minikube-agones.sh`; conserva sus versiones indicadas por el docente y comprueba instalación, recursos y compatibilidad antes de avanzar. No se ha ejecutado ese instalador en esta entrega.

Una vez activo el perfil `agones`, desde la raíz del proyecto en Debian:

```bash
bash Infrastructure/Scripts/build-nfe-image.sh
minikube kubectl -p agones -- get nodes
minikube kubectl -p agones -- get pods -n agones-system
minikube kubectl -p agones -- apply -f Infrastructure/Kubernetes/Agones/nfe-gameserver.yaml
minikube kubectl -p agones -- get gameservers
minikube kubectl -p agones -- get pods -o wide
minikube kubectl -p agones -- logs -l agones.dev/gameserver=networking-lab-nfe -c unity-server
```

El script de construcción específico de NFE **no requiere** que tu compañero haya generado NGO. El manifiesto NFE mantiene UDP 7980 y nodo amd64.

Al abrir Listen, `NfeAgonesLifecycle` detecta `AGONES_SDK_HTTP_PORT`, empieza los latidos `POST /health` y espera el prefab cargado antes de enviar `POST /ready`. Reintenta si el sidecar inicia después. Los logs esperados son `NFE_AGONES: Health accepted` y `NFE_AGONES: Ready accepted`. Confirma además que **el recurso GameServer**, no solo el Pod, llega a `Ready`.

Si falla:

```bash
minikube kubectl -p agones -- describe gameserver networking-lab-nfe
minikube kubectl -p agones -- describe pods -l agones.dev/gameserver=networking-lab-nfe
```

Si reconstruyes el ejecutable o la imagen, vuelve a cargar la imagen y recrea esa instancia de GameServer cuando las pruebas estén detenidas; un Pod existente no cambia su binario por volver a ejecutar `apply`.

## 10. Ruta UDP desde Windows al nodo Minikube

El nodo Minikube con driver Docker normalmente tiene una IP interna distinta de la Host-Only de Debian. El `hostPort` de Agones pertenece al nodo Minikube. **No basta con escribir la IP de un Pod ni con usar `kubectl port-forward`, que no reenvía el tráfico UDP de esta práctica.**

Si Windows tiene una ruta directa validada al endpoint que publica GameServer, usa ese endpoint. Si el nodo Docker no es alcanzable desde Windows, incluye un proxy UDP del laboratorio en Debian. Este helper conserva un socket por cliente y reenvía en ambas direcciones.

En Debian, desde la raíz del proyecto:

```bash
ip -br addr
minikube ip -p agones
python3 Infrastructure/Scripts/forward-nfe-udp.py --listen-address <IP_VM_HOST_ONLY> --target-address "$(minikube ip -p agones)" --listen-port 7980 --target-port 7980
```

Reemplaza el marcador de Host-Only por la IP observada. El proxy debe correr en Debian, **fuera** del contenedor y del nodo Minikube. Mantén su terminal abierta. Si Agones muestra otro puerto en `status.ports`, usa ese número en `--target-port`; el manifiesto entregado utiliza puerto estático 7980.

Ahora Windows conecta a `<IP_VM_HOST_ONLY>:7980`. El helper dirige esos paquetes al nodo Minikube y sus respuestas regresan al cliente correspondiente. No cambia NetCode ni actúa como servidor de juego. Detén el proxy con Ctrl+C al terminar.

El proxy se comprobó con cuatro clientes UDP de prueba, ida y vuelta y puerto de salida estable por cliente. **Esto verifica el helper de red; no reemplaza la prueba real con cuatro clientes Unity.**

## 11. Evidencias de tu parte

| Evidencia | Qué debe verse |
|---|---|
| Código NFE | Las cuatro actividades comentadas y explicación de su flujo. |
| Consola Unity | Compilación sin errores, después de importar en 6000.6.0f1. |
| Entities Hierarchy | Ghosts, GhostOwner e identidad del propietario. |
| Dos y cuatro clientes | Identidades distintas y un jugador controlado por cada proceso. |
| Movimiento | Posiciones compartidas y límites respetados. |
| PowerUp | Mismo número de jugador, una activación por pulsación y mensaje que desaparece. |
| Desconexión | Jugador retirado y UI que deja de mostrar Connected. |
| Build Linux | Carpeta completa y ejecutable ELF x86-64. |
| VM y SSH | `uname -m`, IP Host-Only y conexión remota. |
| Contenedor | Imagen construida, logs y clientes conectados. |
| Kubernetes y Agones | Node Ready, Pods y GameServer Ready. |
| Endpoint y ruta | IP y puerto observados; log del proxy si fue necesario. |
| Demostración final | Cuatro clientes Unity contra el servidor Linux administrado por Agones. |

Marca cada checkpoint como PASS, FAIL, BLOCKED o NOT_TESTED según el resultado que realmente obtengas. Las preguntas de análisis de la guía se responden con tus observaciones y evidencias.

## Fuentes usadas para ajustar las APIs

- [Network connection, Netcode 6.6](https://docs.unity3d.com/Packages/com.unity.netcode@6.6/manual/network-connection.html).
- [SendRpcCommandRequest, Netcode 6.6](https://docs.unity3d.com/Packages/com.unity.netcode@6.6/api/Unity.NetCode.SendRpcCommandRequest.html).
- [Command stream e InputEvent, Netcode 6.6](https://docs.unity3d.com/Packages/com.unity.netcode@6.6/manual/command-stream.html).
- [ClientServerBootstrap, Netcode 6.6](https://docs.unity3d.com/Packages/com.unity.netcode@6.6/api/Unity.NetCode.ClientServerBootstrap.html).
- [Agones REST Game Server Client API](https://agones.dev/site/docs/guides/client-sdks/rest/).
- Guía del docente: `GUIA_LAB_UNITY_NETWORKING_LINUX.pdf`, actividades NFE de las páginas 15 a 18 y capítulos de despliegue.
