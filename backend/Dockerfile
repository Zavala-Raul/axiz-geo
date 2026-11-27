# Multi-stage Dockerfile para ASP.NET Core Web API (.NET 10) en Render
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["AxizGeo.sln", "./"]
COPY ["src/AxizGeo.Domain/AxizGeo.Domain.csproj", "src/AxizGeo.Domain/"]
COPY ["src/AxizGeo.Application/AxizGeo.Application.csproj", "src/AxizGeo.Application/"]
COPY ["src/AxizGeo.Infrastructure/AxizGeo.Infrastructure.csproj", "src/AxizGeo.Infrastructure/"]
COPY ["src/AxizGeo.Api/AxizGeo.Api.csproj", "src/AxizGeo.Api/"]
COPY ["tests/AxizGeo.Tests/AxizGeo.Tests.csproj", "tests/AxizGeo.Tests/"]

RUN dotnet restore "src/AxizGeo.Api/AxizGeo.Api.csproj"

COPY . .
WORKDIR "/src/src/AxizGeo.Api"
RUN dotnet publish "AxizGeo.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AxizGeo.Api.dll"]
