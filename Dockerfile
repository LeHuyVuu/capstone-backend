# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj và restore dependencies
COPY ["capstone-backend/capstone-backend.csproj", "capstone-backend/"]
RUN dotnet restore "capstone-backend/capstone-backend.csproj"

# Copy toàn bộ code và build
COPY . .
WORKDIR "/src/capstone-backend"
RUN dotnet build "capstone-backend.csproj" -c Release -o /app/build
RUN dotnet publish "capstone-backend.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl cho healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5224

# Expose port
EXPOSE 5224

# Healthcheck
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:5224/api/health || exit 1

# Run app
ENTRYPOINT ["dotnet", "capstone-backend.dll"]
