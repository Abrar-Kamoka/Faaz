FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/Shared/Faaz.SharedKernel/Faaz.SharedKernel.csproj src/Shared/Faaz.SharedKernel/
COPY src/Shared/Faaz.BuildingBlocks/Faaz.BuildingBlocks.csproj src/Shared/Faaz.BuildingBlocks/
COPY src/Services/Consultant/Faaz.Services.Consultant.Domain/Faaz.Services.Consultant.Domain.csproj src/Services/Consultant/Faaz.Services.Consultant.Domain/
COPY src/Services/Consultant/Faaz.Services.Consultant.Infrastructure/Faaz.Services.Consultant.Infrastructure.csproj src/Services/Consultant/Faaz.Services.Consultant.Infrastructure/
COPY src/Services/Consultant/Faaz.Services.Consultant.WebHost/Faaz.Services.Consultant.WebHost.csproj src/Services/Consultant/Faaz.Services.Consultant.WebHost/
RUN dotnet restore src/Services/Consultant/Faaz.Services.Consultant.WebHost/Faaz.Services.Consultant.WebHost.csproj

COPY src/Shared/ src/Shared/
COPY src/Services/Consultant/ src/Services/Consultant/
RUN dotnet publish src/Services/Consultant/Faaz.Services.Consultant.WebHost/Faaz.Services.Consultant.WebHost.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Faaz.Services.Consultant.WebHost.dll"]
