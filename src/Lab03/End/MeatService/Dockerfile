FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY MeatService/MeatService.csproj MeatService/
COPY RabbitQueue/RabbitQueue.csproj RabbitQueue/
RUN dotnet restore MeatService/MeatService.csproj
COPY . .
WORKDIR /src/MeatService
RUN dotnet build MeatService.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish MeatService.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "MeatService.dll"]
