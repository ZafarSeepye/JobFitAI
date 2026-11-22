# JobFit AI - container image for hosting (used by Render).
# Stage 1 builds the app with the full .NET SDK; stage 2 runs it on the much smaller ASP.NET runtime image.

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, using only the project file, so Docker can cache the packages between builds.
COPY src/JobFitAI.Web/JobFitAI.Web.csproj src/JobFitAI.Web/
RUN dotnet restore src/JobFitAI.Web/JobFitAI.Web.csproj

# Then copy the code and publish a Release build.
COPY src/ src/
RUN dotnet publish src/JobFitAI.Web/JobFitAI.Web.csproj --configuration Release --no-restore --output /app

# ---------- run ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Production

# Run as the image's built-in non-root user, not as root.
USER $APP_UID

# Render tells the app which port to use through the PORT variable (8080 if it isn't set).
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet JobFitAI.Web.dll"]
