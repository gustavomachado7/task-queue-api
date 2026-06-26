FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["TaskQueue.Api/TaskQueue.Api.csproj", "TaskQueue.Api/"]
COPY ["TaskQueue.Core/TaskQueue.Core.csproj", "TaskQueue.Core/"]
COPY ["TaskQueue.Infrastructure/TaskQueue.Infrastructure.csproj", "TaskQueue.Infrastructure/"]

RUN dotnet restore "TaskQueue.Api/TaskQueue.Api.csproj"

COPY . .

WORKDIR "/src/TaskQueue.Api"
RUN dotnet build "TaskQueue.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TaskQueue.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaskQueue.Api.dll"]