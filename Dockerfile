FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["CourseCore.csproj", "./"]
RUN dotnet restore "CourseCore.csproj"

COPY . .
RUN dotnet publish "CourseCore.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM build AS migrations
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-ef --version 10.0.9

ENTRYPOINT ["dotnet-ef", "database", "update", "--project", "CourseCore.csproj", "--startup-project", "CourseCore.csproj", "--context", "CourseCoreDbContext", "--configuration", "Release", "--no-build"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

USER $APP_UID

ENTRYPOINT ["dotnet", "CourseCore.dll"]
