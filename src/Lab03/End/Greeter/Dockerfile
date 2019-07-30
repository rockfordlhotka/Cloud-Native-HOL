FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY Greeter/Greeter.csproj Greeter/
RUN dotnet restore Greeter/Greeter.csproj
COPY . .
WORKDIR /src/Greeter
RUN dotnet build Greeter.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Greeter.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Greeter.dll"]
