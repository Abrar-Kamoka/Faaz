FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/Shared/Faaz.SharedKernel/Faaz.SharedKernel.csproj src/Shared/Faaz.SharedKernel/
COPY src/Shared/Faaz.BuildingBlocks/Faaz.BuildingBlocks.csproj src/Shared/Faaz.BuildingBlocks/
COPY src/Services/Administration/Faaz.Services.Administration.Domain/Faaz.Services.Administration.Domain.csproj src/Services/Administration/Faaz.Services.Administration.Domain/
COPY src/Services/Administration/Faaz.Services.Administration.Infrastructure/Faaz.Services.Administration.Infrastructure.csproj src/Services/Administration/Faaz.Services.Administration.Infrastructure/
COPY src/Services/Administration/Faaz.Services.Administration.WebHost/Faaz.Services.Administration.WebHost.csproj src/Services/Administration/Faaz.Services.Administration.WebHost/
RUN dotnet restore src/Services/Administration/Faaz.Services.Administration.WebHost/Faaz.Services.Administration.WebHost.csproj

COPY src/Shared/ src/Shared/
COPY src/Services/Administration/ src/Services/Administration/
RUN dotnet publish src/Services/Administration/Faaz.Services.Administration.WebHost/Faaz.Services.Administration.WebHost.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Faaz.Services.Administration.WebHost.dll"]
