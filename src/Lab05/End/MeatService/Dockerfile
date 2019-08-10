FROM mcr.microsoft.com/dotnet/core/aspnet:2.1-stretch-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:2.1-stretch AS build
WORKDIR /src
COPY ["MeatService/MeatService.csproj", "MeatService/"]
RUN dotnet restore "MeatService/MeatService.csproj"
COPY . .
WORKDIR "/src/MeatService"
RUN dotnet build "MeatService.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "MeatService.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "MeatService.dll"]