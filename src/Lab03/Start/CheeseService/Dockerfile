FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY CheeseService/CheeseService.csproj CheeseService/
COPY RabbitQueue/RabbitQueue.csproj RabbitQueue/
RUN dotnet restore CheeseService/CheeseService.csproj
COPY . .
WORKDIR /src/CheeseService
RUN dotnet build CheeseService.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish CheeseService.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "CheeseService.dll"]
