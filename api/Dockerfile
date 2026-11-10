FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ManagementPanel.Api.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish --no-restore --configuration Release --output /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
USER $APP_UID
EXPOSE 10000
ENTRYPOINT ["dotnet", "ManagementPanel.Api.dll"]
