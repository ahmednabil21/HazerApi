# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["HazarApi.sln", "./"]
COPY ["HazarApi/HazarApi.csproj", "HazarApi/"]

# Restore dependencies
RUN dotnet restore "HazarApi/HazarApi.csproj"

# Copy all source files
COPY . .

# Build the application
WORKDIR "/src/HazarApi"
RUN dotnet build "HazarApi.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "HazarApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create logs directory
RUN mkdir -p /app/Logs

# Copy published files
COPY --from=publish /app/publish .

# Expose port (default ASP.NET Core port)
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "HazarApi.dll"]

