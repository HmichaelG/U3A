# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated9.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 8080

# This stage is used to build the service 

RUN apt-get update && apt-get install -y iputils-ping
CMD bash
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src
COPY ["U3A.WebFunctions/U3A.WebFunctions.csproj", "U3A.WebFunctions/"]
COPY ["U3A.Data/U3A.Database.csproj", "U3A.Data/"]
COPY ["U3A.Model/U3A.Model.csproj", "U3A.Model/"]
COPY ["U3A.Services/U3A.Services.csproj", "U3A.Services/"]
COPY ["U3A.UI.Reports/U3A.UI.Reports.csproj", "U3A.UI.Reports/"]
COPY ["U3A.UI.Common/U3A.UI.Common.csproj", "U3A.UI.Common/"]
RUN dotnet restore "./U3A.WebFunctions/U3A.WebFunctions.csproj"
COPY . .
WORKDIR "/src/U3A.WebFunctions"
RUN dotnet build "./U3A.WebFunctions.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./U3A.WebFunctions.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true