FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/Shared/Faaz.SharedKernel/Faaz.SharedKernel.csproj src/Shared/Faaz.SharedKernel/
COPY src/Shared/Faaz.BuildingBlocks/Faaz.BuildingBlocks.csproj src/Shared/Faaz.BuildingBlocks/
COPY src/Services/Notification/Faaz.Services.Notification.Domain/Faaz.Services.Notification.Domain.csproj src/Services/Notification/Faaz.Services.Notification.Domain/
COPY src/Services/Notification/Faaz.Services.Notification.Infrastructure/Faaz.Services.Notification.Infrastructure.csproj src/Services/Notification/Faaz.Services.Notification.Infrastructure/
COPY src/Services/Notification/Faaz.Services.Notification.WebHost/Faaz.Services.Notification.WebHost.csproj src/Services/Notification/Faaz.Services.Notification.WebHost/
RUN dotnet restore src/Services/Notification/Faaz.Services.Notification.WebHost/Faaz.Services.Notification.WebHost.csproj

COPY src/Shared/ src/Shared/
COPY src/Services/Notification/ src/Services/Notification/
RUN dotnet publish src/Services/Notification/Faaz.Services.Notification.WebHost/Faaz.Services.Notification.WebHost.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Faaz.Services.Notification.WebHost.dll"]