FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["TaskQueue.Worker/TaskQueue.Worker.csproj", "TaskQueue.Worker/"]
COPY ["TaskQueue.Core/TaskQueue.Core.csproj", "TaskQueue.Core/"]
COPY ["TaskQueue.Infrastructure/TaskQueue.Infrastructure.csproj", "TaskQueue.Infrastructure/"]

RUN dotnet restore "TaskQueue.Worker/TaskQueue.Worker.csproj"

COPY . .

WORKDIR "/src/TaskQueue.Worker"
RUN dotnet build "TaskQueue.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TaskQueue.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaskQueue.Worker.dll"]