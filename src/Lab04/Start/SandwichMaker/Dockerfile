FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY SandwichMaker/SandwichMaker.csproj SandwichMaker/
COPY RabbitQueue/RabbitQueue.csproj RabbitQueue/
RUN dotnet restore SandwichMaker/SandwichMaker.csproj
COPY . .
WORKDIR /src/SandwichMaker
RUN dotnet build SandwichMaker.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish SandwichMaker.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SandwichMaker.dll"]
