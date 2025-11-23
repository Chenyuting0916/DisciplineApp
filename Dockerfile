# Base image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Build image
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["DisciplineApp.csproj", "./"]
RUN dotnet restore "DisciplineApp.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "DisciplineApp.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "DisciplineApp.csproj" -c Release -o /app/publish

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DisciplineApp.dll"]
