FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/Shared/Faaz.SharedKernel/Faaz.SharedKernel.csproj src/Shared/Faaz.SharedKernel/
COPY src/Shared/Faaz.BuildingBlocks/Faaz.BuildingBlocks.csproj src/Shared/Faaz.BuildingBlocks/
COPY src/Services/Booking/Faaz.Services.Booking.Domain/Faaz.Services.Booking.Domain.csproj src/Services/Booking/Faaz.Services.Booking.Domain/
COPY src/Services/Booking/Faaz.Services.Booking.Infrastructure/Faaz.Services.Booking.Infrastructure.csproj src/Services/Booking/Faaz.Services.Booking.Infrastructure/
COPY src/Services/Booking/Faaz.Services.Booking.WebHost/Faaz.Services.Booking.WebHost.csproj src/Services/Booking/Faaz.Services.Booking.WebHost/
RUN dotnet restore src/Services/Booking/Faaz.Services.Booking.WebHost/Faaz.Services.Booking.WebHost.csproj

COPY src/Shared/ src/Shared/
COPY src/Services/Booking/ src/Services/Booking/
RUN dotnet publish src/Services/Booking/Faaz.Services.Booking.WebHost/Faaz.Services.Booking.WebHost.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Faaz.Services.Booking.WebHost.dll"]
