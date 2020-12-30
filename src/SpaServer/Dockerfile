#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
#EXPOSE 80
#EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["SpaServer/SpaServer.csproj", "SpaServer/"]
RUN dotnet restore "SpaServer/SpaServer.csproj"
COPY . .
WORKDIR "/src/SpaServer"
RUN dotnet build "SpaServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SpaServer.csproj" -c Release -o /app/publish

# build Vue app:
FROM node:alpine as buildvue
WORKDIR /src

ARG skip_client_build=false 
COPY SpaServer/ClientApp/package.json .
RUN [[ ${skip_client_build} = true ]] && echo "Skipping npm install" || npm install

# webpack build
COPY SpaServer/ClientApp .
RUN [[ ${skip_client_build} = true ]] && mkdir ClientApp/dist/spa || npm run build

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=buildvue /src/dist/spa /app/ClientApp/dist/spa
ENTRYPOINT ["dotnet", "SpaServer.dll"]