
# Stage 1: Build and publish the .NET 10 API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Central package and build configuration
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY global.json ./

# Copy project files for dependency restoration
COPY apps/api/src/SupportDesk.Domain/SupportDesk.Domain.csproj apps/api/src/SupportDesk.Domain/
COPY apps/api/src/SupportDesk.Application/SupportDesk.Application.csproj apps/api/src/SupportDesk.Application/
COPY apps/api/src/SupportDesk.Infrastructure/SupportDesk.Infrastructure.csproj apps/api/src/SupportDesk.Infrastructure/
COPY apps/api/src/SupportDesk.Presentation/SupportDesk.Presentation.csproj apps/api/src/SupportDesk.Presentation/

# Restore dependencies
RUN dotnet restore apps/api/src/SupportDesk.Presentation/SupportDesk.Presentation.csproj

# Copy the source code
COPY apps/api/src/ apps/api/src/

# Publish the application
RUN dotnet publish apps/api/src/SupportDesk.Presentation/SupportDesk.Presentation.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    --no-self-contained


# Stage 2: Run with the ASP.NET Core runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

# Container listens on HTTP port 8080.
# The environment name and secrets are supplied at runtime.
ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

COPY --from=build /app/publish .

# Run as the non-root user provided by the .NET image.
USER $APP_UID

ENTRYPOINT ["dotnet", "SupportDesk.Presentation.dll"]