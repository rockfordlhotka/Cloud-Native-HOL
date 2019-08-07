FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
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
