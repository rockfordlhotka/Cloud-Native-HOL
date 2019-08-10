FROM mcr.microsoft.com/dotnet/core/aspnet:2.1-stretch-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:2.1-stretch AS build
WORKDIR /src
COPY ["SandwichMaker/SandwichMaker.csproj", "SandwichMaker/"]
RUN dotnet restore "SandwichMaker/SandwichMaker.csproj"
COPY . .
WORKDIR "/src/SandwichMaker"
RUN dotnet build "SandwichMaker.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "SandwichMaker.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SandwichMaker.dll"]