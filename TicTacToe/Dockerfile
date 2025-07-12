# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
#FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
## USER $APP_UID
#WORKDIR /app
#EXPOSE 8080
#
## Этот этап используется для сборки проекта службы
#FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
#WORKDIR /src
#COPY ["TicTacToe.csproj", "."]
#RUN rm -f /usr/share/dotnet/sdk/9.0.302/dotnet.runtimeconfig.json && \
    #dotnet restore "TicTacToe.csproj"
#COPY . .
## WORKDIR "/src/TicTacToe"
#RUN dotnet build "TicTacToe.csproj" -c Release  -o /app/build
#
## Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
#FROM build AS publish
#RUN dotnet publish "TicTacToe.csproj" -c Release  -o /app/publish
#
## Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "TicTacToe.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Явно указываем архитектуру
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Добавляем поддержку ICU (для работы с культурами)
RUN apt-get update && \
    apt-get install -y libicu-dev && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY ["TicTacToe.csproj", "."]
RUN dotnet restore "TicTacToe.csproj" --runtime linux-x64
COPY . .
RUN dotnet build "TicTacToe.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TicTacToe.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TicTacToe.dll"]