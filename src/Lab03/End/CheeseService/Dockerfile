FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
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
