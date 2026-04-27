FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/PulseLog.Api/PulseLog.Api.csproj", "PulseLog.Api/"]
RUN dotnet restore "PulseLog.Api/PulseLog.Api.csproj"
COPY . .
WORKDIR "/src/PulseLog.Api"
RUN dotnet publish -c release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Development
ENTRYPOINT ["dotnet", "PulseLog.Api.dll"]