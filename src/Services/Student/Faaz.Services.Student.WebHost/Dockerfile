FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/Shared/Faaz.SharedKernel/Faaz.SharedKernel.csproj src/Shared/Faaz.SharedKernel/
COPY src/Shared/Faaz.BuildingBlocks/Faaz.BuildingBlocks.csproj src/Shared/Faaz.BuildingBlocks/
COPY src/Services/Student/Faaz.Services.Student.Domain/Faaz.Services.Student.Domain.csproj src/Services/Student/Faaz.Services.Student.Domain/
COPY src/Services/Student/Faaz.Services.Student.Infrastructure/Faaz.Services.Student.Infrastructure.csproj src/Services/Student/Faaz.Services.Student.Infrastructure/
COPY src/Services/Student/Faaz.Services.Student.WebHost/Faaz.Services.Student.WebHost.csproj src/Services/Student/Faaz.Services.Student.WebHost/
RUN dotnet restore src/Services/Student/Faaz.Services.Student.WebHost/Faaz.Services.Student.WebHost.csproj

COPY src/Shared/ src/Shared/
COPY src/Services/Student/ src/Services/Student/
RUN dotnet publish src/Services/Student/Faaz.Services.Student.WebHost/Faaz.Services.Student.WebHost.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Faaz.Services.Student.WebHost.dll"]
