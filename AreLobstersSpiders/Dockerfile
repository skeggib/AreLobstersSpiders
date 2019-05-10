FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build
COPY AreLobstersSpiders.csproj /app/
WORKDIR /app
RUN dotnet restore
COPY *.cs /app/
RUN dotnet publish -c Release

FROM mcr.microsoft.com/dotnet/core/runtime:2.2
COPY --from=build /app/bin/Release/netcoreapp2.2/publish /app
WORKDIR /app
ENTRYPOINT ["dotnet", "AreLobstersSpiders.dll"]