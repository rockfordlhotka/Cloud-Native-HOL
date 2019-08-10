FROM mcr.microsoft.com/dotnet/core/aspnet:2.1-stretch-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:2.1-stretch AS build
WORKDIR /src
COPY ["CheeseService/CheeseService.csproj", "CheeseService/"]
RUN dotnet restore "CheeseService/CheeseService.csproj"
COPY . .
WORKDIR "/src/CheeseService"
RUN dotnet build "CheeseService.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "CheeseService.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "CheeseService.dll"]