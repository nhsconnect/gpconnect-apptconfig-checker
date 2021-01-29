FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
RUN apt-get update && apt-get install -y libldap-2.4-2
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build
WORKDIR /src
COPY gpconnect-appointment-checker.sln ./
COPY gpconnect-appointment-checker.Console/*.csproj ./gpconnect-appointment-checker.Console/
COPY gpconnect-appointment-checker.IntegrationTest/*.csproj ./gpconnect-appointment-checker.IntegrationTest/
COPY gpconnect-appointment-checker.Helpers/*.csproj ./gpconnect-appointment-checker.Helpers/
COPY gpconnect-appointment-checker.DTO/*.csproj ./gpconnect-appointment-checker.DTO/
COPY gpconnect-appointment-checker.DAL/*.csproj ./gpconnect-appointment-checker.DAL/
COPY gpconnect-appointment-checker.GPConnect/*.csproj ./gpconnect-appointment-checker.GPConnect/
COPY gpconnect-appointment-checker.SDS/*.csproj ./gpconnect-appointment-checker.SDS/
COPY gpconnect-appointment-checker.AWSLambda/*.csproj ./gpconnect-appointment-checker.AWSLambda/
COPY gpconnect-appointment-checker.Caching/*.csproj ./gpconnect-appointment-checker.Caching/
COPY gpconnect-appointment-checker/*.csproj ./gpconnect-appointment-checker/

RUN dotnet restore
COPY . .

WORKDIR /src
RUN dotnet build -c Debug -o /app

FROM build AS publish
RUN dotnet publish -c Debug -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

RUN sed -i "s|DEFAULT@SECLEVEL=2|DEFAULT@SECLEVEL=1|g" /etc/ssl/openssl.cnf

ENTRYPOINT ["dotnet", "gpconnect-appointment-checker.dll"]