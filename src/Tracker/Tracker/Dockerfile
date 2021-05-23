FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS publish
WORKDIR /src
COPY ["Tracker/Tracker.csproj", "Tracker/"]
COPY ["Tracker.DAL/Tracker.DAL.csproj", "Tracker.DAL/"]
COPY ["Shared/Shared.csproj", "Shared/"]
COPY ["Battlelog/Battlelog.csproj", "Battlelog/"]

RUN dotnet restore "Tracker/Tracker.csproj" --runtime alpine-x64
COPY . .
WORKDIR "/src/Tracker"
RUN dotnet publish "Tracker.csproj" -c Release -o /app/publish \
  --no-restore \
  --runtime alpine-x64 \
  --self-contained true \
  /p:PublishTrimmed=true \
  /p:PublishSingleFile=true

FROM mcr.microsoft.com/dotnet/runtime-deps:5.0-alpine AS final

RUN adduser --disabled-password \
  --home /app \
  --gecos '' dotnetuser && chown -R dotnetuser /app

# upgrade musl to remove potential vulnerability
RUN apk upgrade musl

USER dotnetuser
WORKDIR /app
EXPOSE 5000
COPY --from=publish /app/publish .

ENTRYPOINT ["./Tracker"]