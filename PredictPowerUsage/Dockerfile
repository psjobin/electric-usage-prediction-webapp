FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["PredictPowerUsage/PredictPowerUsage.csproj", "PredictPowerUsage/"]
RUN dotnet restore "PredictPowerUsage/PredictPowerUsage.csproj"
COPY . .
WORKDIR "/src/PredictPowerUsage"
RUN dotnet build "PredictPowerUsage.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PredictPowerUsage.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "PredictPowerUsage.dll"]
CMD ASPNETCORE_URLS=http://*:$PORT dotnet PredictPowerUsage.dll


