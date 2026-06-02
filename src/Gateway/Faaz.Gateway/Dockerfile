FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/Gateway/Faaz.Gateway/Faaz.Gateway.csproj src/Gateway/Faaz.Gateway/
RUN dotnet restore src/Gateway/Faaz.Gateway/Faaz.Gateway.csproj

COPY src/Gateway/ src/Gateway/
RUN dotnet publish src/Gateway/Faaz.Gateway/Faaz.Gateway.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Faaz.Gateway.dll"]
