# ── Etapa 1: Compilación y Publicación ──
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar archivos de proyecto y restaurar dependencias
COPY ["LentSoft.Web/LentSoft.Web.csproj", "LentSoft.Web/"]
RUN dotnet restore "LentSoft.Web/LentSoft.Web.csproj"

# Copiar todo el código fuente y publicar en modo Release
COPY . .
WORKDIR "/src/LentSoft.Web"
RUN dotnet publish "LentSoft.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ── Etapa 2: Imagen Final de Ejecución ──
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Instalar dependencias nativas requeridas por QuestPDF y SkiaSharp en Linux
RUN apt-get update && apt-get install -y --no-install-recommends \
    libfontconfig1 \
    libfreetype6 \
    libharfbuzz0b \
    libicu-dev \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "LentSoft.Web.dll"]
