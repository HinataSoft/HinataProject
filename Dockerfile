FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

RUN apt update \
 && apt --yes install \
    fonts-liberation locales \
    ca-certificates procps net-tools less \
 && apt-get autoclean \
 && apt-get autoremove \
 && rm -rf /var/lib/apt/lists/* \
 && sed -i -e 's/# cs_CZ.UTF-8 UTF-8/cs_CZ.UTF-8 UTF-8/' /etc/locale.gen \
 && dpkg-reconfigure --frontend=noninteractive locales \
 && update-locale LANG=cs_CZ.UTF-8

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY . .

ARG nuget_source
ARG nuget_username
ARG nuget_secret

RUN dotnet nuget add source $nuget_source                      --name NugetSource  --username $nuget_username       --password $nuget_secret  --store-password-in-clear-text
RUN dotnet restore "Api/Api.csproj"

RUN ls -la ./
WORKDIR Api
RUN ls -la ./
RUN dotnet build "Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "HinataProject.Api.dll"]
