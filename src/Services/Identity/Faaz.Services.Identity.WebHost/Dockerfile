FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/Shared/Faaz.SharedKernel/Faaz.SharedKernel.csproj src/Shared/Faaz.SharedKernel/
COPY src/Shared/Faaz.BuildingBlocks/Faaz.BuildingBlocks.csproj src/Shared/Faaz.BuildingBlocks/
COPY src/Services/Identity/Faaz.Services.Identity.Domain/Faaz.Services.Identity.Domain.csproj src/Services/Identity/Faaz.Services.Identity.Domain/
COPY src/Services/Identity/Faaz.Services.Identity.Infrastructure/Faaz.Services.Identity.Infrastructure.csproj src/Services/Identity/Faaz.Services.Identity.Infrastructure/
COPY src/Services/Identity/Faaz.Services.Identity.WebHost/Faaz.Services.Identity.WebHost.csproj src/Services/Identity/Faaz.Services.Identity.WebHost/
RUN dotnet restore src/Services/Identity/Faaz.Services.Identity.WebHost/Faaz.Services.Identity.WebHost.csproj

COPY src/Shared/ src/Shared/
COPY src/Services/Identity/ src/Services/Identity/
RUN dotnet publish src/Services/Identity/Faaz.Services.Identity.WebHost/Faaz.Services.Identity.WebHost.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Faaz.Services.Identity.WebHost.dll"]
