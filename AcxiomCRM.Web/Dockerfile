FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AcxiomCRM.Web/AcxiomCRM.Web.csproj", "AcxiomCRM.Web/"]
COPY ["AcxiomCRM.Tests/AcxiomCRM.Tests.csproj", "AcxiomCRM.Tests/"]
RUN dotnet restore "AcxiomCRM.Web/AcxiomCRM.Web.csproj"
COPY . .
WORKDIR "/src/AcxiomCRM.Web"
RUN dotnet publish "AcxiomCRM.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "AcxiomCRM.Web.dll"]
