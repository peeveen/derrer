# Version of .NET SDK required.
ARG dotnetSdkVersion="8.0"
# Version of webservice being built.
ARG version="1.0.0.0"

# This container builds the code.
FROM mcr.microsoft.com/dotnet/sdk:${dotnetSdkVersion} AS build
ARG version
WORKDIR /app
# Copy the main project (no test/benchmark code) into that folder.
COPY ./* ./
# Now build! We'll target alpine, as it's nice and wee.
RUN dotnet publish -c Release -r linux-musl-x64 --self-contained /p:Version=${version}

# This container is what the built code is published into.
FROM alpine:latest
ARG dotnetSdkVersion
# Install the packages we need, plus any additional packages that are defined.
RUN apk add gcompat libstdc++ icu-libs
# Everything is going into this folder.
WORKDIR /derrer
RUN mkdir ./ssl
# Set HTTP port
ENV ASPNETCORE_HTTP_PORT 8080
# Get all the DLLs/EXEs/etc.
COPY --from=build /app/bin/Release/net${dotnetSdkVersion}/linux-musl-x64/publish/ ./
# Get the appsettings file.
COPY --from=build /app/appsettings.json ./
# The entrypoint will be our webservice executable.
ENTRYPOINT ["/derrer/derrer-service"]
