FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["iOSBot.Bot/iOSBot.Bot.csproj", "iOSBot.Bot/"]
COPY ["iOSBot.Service/iOSBot.Service.csproj", "iOSBot.Service/"]
COPY ["iOSBot.Data/iOSBot.Data.csproj", "iOSBot.Data/"]
RUN dotnet restore "iOSBot.Bot/iOSBot.Bot.csproj"
COPY . .
WORKDIR "/src/iOSBot.Bot"
RUN dotnet build "./iOSBot.Bot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./iOSBot.Bot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "iOSBot.Bot.dll"]
