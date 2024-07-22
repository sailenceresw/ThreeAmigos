# Use the official ASP.NET Core runtime as a base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the official .NET SDK as a build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ecommerce.csproj", "."]
RUN dotnet restore "ecommerce.csproj"
COPY . .
RUN dotnet build "ecommerce.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ecommerce.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage: copy the build output to the base image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ecommerce.dll"]
