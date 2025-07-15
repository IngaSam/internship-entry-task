#FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
#WORKDIR /app
#EXPOSE 8080
#
## Явно указываем архитектуру
#FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:9.0 AS build
#
## Добавляем поддержку ICU (для работы с культурами)
#RUN apt-get update && \
    #apt-get install -y libicu-dev && \
    #rm -rf /var/lib/apt/lists/*
#
#WORKDIR /src
#COPY ["TicTacToe.csproj", "."]
#RUN dotnet restore "TicTacToe.csproj" --runtime linux-x64
#COPY . .
#RUN dotnet build "TicTacToe.csproj" -c Release -o /app/build
#
#FROM build AS publish
#RUN dotnet publish "TicTacToe.csproj" -c Release -o /app/publish
#
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "TicTacToe.dll"]

# Используем многоступенчатую сборку
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем только файлы проекта для кэширования слоев
COPY ["TicTacToe.csproj", "Directory.Build.props", "./"]
RUN dotnet restore "TicTacToe.csproj"

# Копируем остальные файлы и собираем
COPY . .
RUN dotnet build "TicTacToe.csproj" -c Release -o /app/build

# Этап публикации
FROM build AS publish
RUN dotnet publish "TicTacToe.csproj" -c Release -o /app/publish \
    --no-restore \
    --runtime linux-x64 \
    --self-contained false

# Финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Устанавливаем ICU для локализации
RUN apt-get update && \
    apt-get install -y --no-install-recommends libicu-dev && \
    rm -rf /var/lib/apt/lists/*

# Копируем результаты публикации
COPY --from=publish /app/publish .

# Настройки среды по умолчанию
ENV ASPNETCORE_URLS=http://+:8080 \
    BOARD_SIZE=3 \
    WIN_LENGTH=3 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TicTacToe.dll"]