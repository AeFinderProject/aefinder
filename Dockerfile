FROM mcr.microsoft.com/dotnet/sdk:7.0.407
ARG servicename
WORKDIR /app
COPY out/$servicename .