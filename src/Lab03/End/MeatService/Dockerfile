FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
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
