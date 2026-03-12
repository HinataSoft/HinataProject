# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish --no-restore Api/Api.csproj

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install SSL certificates if needed (for production use)
# RUN apt-get update && apt-get install -y --no-install-recommends \
#     ca-certificates \
#     && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Expose the default port
EXPOSE 8080

# Set the entry point
ENTRYPOINT ["dotnet", "HinataProject.Api.dll"]
