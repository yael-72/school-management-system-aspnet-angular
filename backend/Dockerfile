# Multi-stage build: the SDK image compiles the API, the much smaller ASP.NET runtime image runs it.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, using only the project files, so Docker can cache the NuGet packages
# and skip the restore when only source code changed.
COPY Directory.Build.props SchoolManagement.sln ./
COPY src/SchoolManagement.Api/SchoolManagement.Api.csproj src/SchoolManagement.Api/
COPY src/SchoolManagement.Application/SchoolManagement.Application.csproj src/SchoolManagement.Application/
COPY src/SchoolManagement.Domain/SchoolManagement.Domain.csproj src/SchoolManagement.Domain/
COPY src/SchoolManagement.Infrastructure/SchoolManagement.Infrastructure.csproj src/SchoolManagement.Infrastructure/
RUN dotnet restore src/SchoolManagement.Api/SchoolManagement.Api.csproj

COPY src/ src/
RUN dotnet publish src/SchoolManagement.Api/SchoolManagement.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Uploaded files live here (mounted as a volume in docker-compose).
RUN mkdir -p /app/App_Data/uploads && chown -R $APP_UID /app/App_Data

# The official image has a non-root "app" user; never run the web server as root.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SchoolManagement.Api.dll"]
