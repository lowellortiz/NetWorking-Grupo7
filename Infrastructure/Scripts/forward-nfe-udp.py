#!/usr/bin/env python3
"""Proxy UDP para Windows -> IP Host-Only de Debian -> nodo Minikube.

Conserva un socket de salida por cliente; los paquetes de una conexión
mantienen su puerto de origen y las respuestas vuelven al cliente correcto.
No reemplaza la réplica ni la validación de NetCode.
"""

import argparse
import ipaddress
import selectors
import socket
import time


def ipv4(value):
    try:
        return str(ipaddress.IPv4Address(value))
    except ipaddress.AddressValueError as exc:
        raise argparse.ArgumentTypeError("Se requiere una dirección IPv4.") from exc


def port(value):
    try:
        number = int(value)
    except ValueError as exc:
        raise argparse.ArgumentTypeError("El puerto debe ser un número.") from exc
    if not 1 <= number <= 65535:
        raise argparse.ArgumentTypeError("El puerto debe estar entre 1 y 65535.")
    return number


def serve(listen_address, listen_port, target_address, target_port, idle_seconds=300):
    selector = selectors.DefaultSelector()
    frontend = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    peers = {}

    def remove_peer(address):
        entry = peers.pop(address, None)
        if entry is not None:
            selector.unregister(entry["socket"])
            entry["socket"].close()

    try:
        frontend.bind((listen_address, listen_port))
        frontend.setblocking(False)
        selector.register(frontend, selectors.EVENT_READ, None)
        print(f"NFE_UDP_PROXY: {listen_address}:{listen_port} -> {target_address}:{target_port}", flush=True)
        while True:
            for key, _ in selector.select(timeout=0.5):
                if key.fileobj is frontend:
                    payload, address = frontend.recvfrom(65535)
                    if address not in peers:
                        backend = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
                        backend.setblocking(False)
                        backend.connect((target_address, target_port))
                        peers[address] = {"socket": backend, "last_seen": time.monotonic()}
                        selector.register(backend, selectors.EVENT_READ, address)
                        print(f"NFE_UDP_PROXY: client {address[0]}:{address[1]}", flush=True)
                    peers[address]["last_seen"] = time.monotonic()
                    try:
                        peers[address]["socket"].send(payload)
                    except OSError:
                        remove_peer(address)
                else:
                    address = key.data
                    try:
                        payload = key.fileobj.recv(65535)
                        frontend.sendto(payload, address)
                        peers[address]["last_seen"] = time.monotonic()
                    except OSError:
                        remove_peer(address)

            now = time.monotonic()
            for address, entry in list(peers.items()):
                if now - entry["last_seen"] > idle_seconds:
                    remove_peer(address)
    except KeyboardInterrupt:
        print("NFE_UDP_PROXY: stopped", flush=True)
    finally:
        for address in list(peers):
            remove_peer(address)
        selector.close()
        frontend.close()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--listen-address", type=ipv4, required=True, help="IPv4 Host-Only de Debian")
    parser.add_argument("--listen-port", type=port, default=7980)
    parser.add_argument("--target-address", type=ipv4, required=True, help="IPv4 de minikube ip -p agones")
    parser.add_argument("--target-port", type=port, default=7980)
    args = parser.parse_args()
    serve(args.listen_address, args.listen_port, args.target_address, args.target_port)


if __name__ == "__main__":
    main()
