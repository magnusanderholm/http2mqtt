#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

# Map this in docker if we want to access as HTTP
EXPOSE 5137 
ENV ASPNETCORE_URLS=http://+:5004
# Map this in docker if we want to access as HTTPS. Requires a valid certificate in that case. 
# Its not useful  right now since we are supposed to put this behind a reverse proxy which will handle certificates (app does not support it yet.)
#EXPOSE 7137 

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["http2mqtt.csproj", "./"]
RUN dotnet restore "http2mqtt.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "http2mqtt.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "http2mqtt.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "http2mqtt.dll"]