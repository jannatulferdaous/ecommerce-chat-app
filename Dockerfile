FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["backend/EcommerceChat.Api/EcommerceChat.Api.csproj", "backend/EcommerceChat.Api/"]
RUN dotnet restore "backend/EcommerceChat.Api/EcommerceChat.Api.csproj"
COPY . .
WORKDIR "/src/backend/EcommerceChat.Api"
RUN dotnet build "EcommerceChat.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EcommerceChat.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EcommerceChat.Api.dll"]