
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY src/RaceDay.Api/RaceDay.Api.csproj src/RaceDay.Api/

RUN dotnet restore src/RaceDay.Api/RaceDay.Api.csproj

COPY src/RaceDay.Api/ src/RaceDay.Api/

RUN dotnet restore src/RaceDay.Api/RaceDay.Api.csproj

RUN dotnet publish src/RaceDay.Api/RaceDay.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "RaceDay.Api.dll"]
