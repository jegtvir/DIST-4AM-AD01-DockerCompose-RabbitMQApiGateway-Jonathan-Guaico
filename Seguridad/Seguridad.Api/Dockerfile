FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Seguridad.Api.csproj", "./"]
RUN dotnet restore "Seguridad.Api.csproj"
COPY . .
RUN dotnet build "Seguridad.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Seguridad.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Seguridad.Api.dll"]
