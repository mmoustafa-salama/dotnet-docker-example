#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ENV Caching="Redis-from-base"

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["dotnet-docker-example.csproj", "."]
RUN dotnet restore "./dotnet-docker-example.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "dotnet-docker-example.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "dotnet-docker-example.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV Caching="Redis-from-final"
ENTRYPOINT ["dotnet", "dotnet-docker-example.dll"]