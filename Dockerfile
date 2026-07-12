FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["SportCourtManagement_FrontEnd/SportCourtManagement_FrontEnd.csproj", "SportCourtManagement_FrontEnd/"]
RUN dotnet restore "SportCourtManagement_FrontEnd/SportCourtManagement_FrontEnd.csproj"

# Copy all source code
COPY . .

WORKDIR "/src/SportCourtManagement_FrontEnd"
RUN dotnet build "SportCourtManagement_FrontEnd.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SportCourtManagement_FrontEnd.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SportCourtManagement_FrontEnd.dll"]
