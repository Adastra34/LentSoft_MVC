# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar archivos de solución y proyectos para restaurar dependencias
COPY ["LentSoft.sln", "./"]
COPY ["LentSoft.Web/LentSoft.Web.csproj", "LentSoft.Web/"]
COPY ["LentSoft.Tests/LentSoft.Tests.csproj", "LentSoft.Tests/"]

RUN dotnet restore "LentSoft.Web/LentSoft.Web.csproj"

# Copiar todo el código fuente
COPY . .

# Compilar y publicar la aplicación web
WORKDIR "/src/LentSoft.Web"
RUN dotnet publish "LentSoft.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "LentSoft.Web.dll"]
