FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/Shared/Faaz.SharedKernel/Faaz.SharedKernel.csproj src/Shared/Faaz.SharedKernel/
COPY src/Shared/Faaz.BuildingBlocks/Faaz.BuildingBlocks.csproj src/Shared/Faaz.BuildingBlocks/
COPY src/Services/Payment/Faaz.Services.Payment.Domain/Faaz.Services.Payment.Domain.csproj src/Services/Payment/Faaz.Services.Payment.Domain/
COPY src/Services/Payment/Faaz.Services.Payment.Infrastructure/Faaz.Services.Payment.Infrastructure.csproj src/Services/Payment/Faaz.Services.Payment.Infrastructure/
COPY src/Services/Payment/Faaz.Services.Payment.WebHost/Faaz.Services.Payment.WebHost.csproj src/Services/Payment/Faaz.Services.Payment.WebHost/
RUN dotnet restore src/Services/Payment/Faaz.Services.Payment.WebHost/Faaz.Services.Payment.WebHost.csproj

COPY src/Shared/ src/Shared/
COPY src/Services/Payment/ src/Services/Payment/
RUN dotnet publish src/Services/Payment/Faaz.Services.Payment.WebHost/Faaz.Services.Payment.WebHost.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Faaz.Services.Payment.WebHost.dll"]
