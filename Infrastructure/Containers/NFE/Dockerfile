FROM debian:13-slim

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates libatomic1 libunwind8 \
    && rm -rf /var/lib/apt/lists/* \
    && useradd --create-home --uid 10001 unity

WORKDIR /home/unity/server
COPY Builds/NFE-LinuxServer/ ./
RUN chmod +x NetworkingLabNFEServer.x86_64 \
    && chown -R unity:unity /home/unity/server

USER unity
EXPOSE 7980/udp
ENTRYPOINT ["./NetworkingLabNFEServer.x86_64", "-batchmode", "-nographics", "-nfe", "-port", "7980"]
