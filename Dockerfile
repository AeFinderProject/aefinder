FROM mcr.microsoft.com/dotnet/sdk:7.0.407 # Use the latest version available
ARG servicename
WORKDIR /app
COPY out/$servicename .