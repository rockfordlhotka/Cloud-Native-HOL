FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY LettuceService/LettuceService.csproj LettuceService/
COPY RabbitQueue/RabbitQueue.csproj RabbitQueue/
RUN dotnet restore LettuceService/LettuceService.csproj
COPY . .
WORKDIR /src/LettuceService
RUN dotnet build LettuceService.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish LettuceService.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "LettuceService.dll"]
