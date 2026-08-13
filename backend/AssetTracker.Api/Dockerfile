FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the .csproj files first (preserving relative directory structure so
# project references resolve) and restore. This keeps the restore layer cached
# across builds where only source files change, avoiding a full NuGet restore
# on every source-code edit.
COPY AssetTracker.Domain/AssetTracker.Domain.csproj AssetTracker.Domain/
COPY AssetTracker.Application/AssetTracker.Application.csproj AssetTracker.Application/
COPY AssetTracker.Infrastructure/AssetTracker.Infrastructure.csproj AssetTracker.Infrastructure/
COPY AssetTracker.Api/AssetTracker.Api.csproj AssetTracker.Api/
RUN dotnet restore AssetTracker.Api/AssetTracker.Api.csproj

# Now copy the full source and publish.
COPY . .
RUN dotnet publish AssetTracker.Api/AssetTracker.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "AssetTracker.Api.dll"]
