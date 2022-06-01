#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MeatService/MeatService.csproj", "MeatService/"]
COPY ["RabbitQueue/RabbitQueue.csproj", "RabbitQueue/"]
RUN dotnet restore "MeatService/MeatService.csproj"
COPY . .
WORKDIR "/src/MeatService"
RUN dotnet build "MeatService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MeatService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MeatService.dll"]