FROM debian:13-slim

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates libatomic1 libunwind8 \
    && rm -rf /var/lib/apt/lists/* \
    && useradd --create-home --uid 10001 unity

WORKDIR /home/unity/server
COPY Builds/NGO-LinuxServer/ ./
RUN chmod +x NetworkingLabNGOServer.x86_64 \
    && chown -R unity:unity /home/unity/server

USER unity
EXPOSE 7979/udp
ENTRYPOINT ["./NetworkingLabNGOServer.x86_64", "-batchmode", "-nographics", "-port", "7979"]
