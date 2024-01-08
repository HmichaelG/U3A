#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated7.0 AS base
RUN apt-get update
RUN apt-get install -y wait-for-it libc6 libicu-dev libfontconfig1 libgdiplus libharfbuzz0b


WORKDIR /home/site/wwwroot
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["U3A.WebFunctions/U3A.WebFunctions.csproj", "U3A.WebFunctions/"]
COPY ["U3A.Data/U3A.Database.csproj", "U3A.Data/"]
COPY ["U3A.Model/U3A.Model.csproj", "U3A.Model/"]
COPY ["U3A.Services/U3A.Services.csproj", "U3A.Services/"]
COPY ["U3A.UI.Reports/U3A.UI.Reports.csproj", "U3A.UI.Reports/"]
RUN dotnet restore "U3A.WebFunctions/U3A.WebFunctions.csproj"
COPY . .
WORKDIR "/src/U3A.WebFunctions"
RUN dotnet build "U3A.WebFunctions.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "U3A.WebFunctions.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true