#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["DockerApi/DockerApi.csproj", "DockerApi/"]
COPY ["Backend/DreamTrips/DreamTrips.csproj", "Backend/DreamTrips/"]
COPY ["Backend/TravelingSalesmanProblem/TravelingSalesmanProblem.csproj", "Backend/TravelingSalesmanProblem/"]
COPY ["Backend/GeolocationData/Clients.csproj", "Backend/GeolocationData/"]
COPY ["Backend/Domain/Domain.csproj", "Backend/Domain/"]
RUN dotnet restore "DockerApi/DockerApi.csproj"
COPY . .
WORKDIR "/src/DockerApi"
RUN dotnet build "DockerApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DockerApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DockerApi.dll"]
