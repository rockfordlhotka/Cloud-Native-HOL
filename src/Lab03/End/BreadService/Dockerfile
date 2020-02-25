FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY BreadService/BreadService.csproj BreadService/
COPY RabbitQueue/RabbitQueue.csproj RabbitQueue/
RUN dotnet restore BreadService/BreadService.csproj
COPY . .
WORKDIR /src/BreadService
RUN dotnet build BreadService.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish BreadService.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "BreadService.dll"]
