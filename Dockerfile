FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY PersonalFinanceTracker.Api.csproj ./
RUN dotnet restore PersonalFinanceTracker.Api.csproj

COPY . .
RUN dotnet publish PersonalFinanceTracker.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app

COPY --from=build /app/publish .
USER app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PersonalFinanceTracker.Api.dll"]
