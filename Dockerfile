FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Zeno/Zeno.API.csproj", "Zeno/"]
COPY ["Zeno.Application/Zeno.Application.csproj", "Zeno.Application/"]
COPY ["Zeno.Domain/Zeno.Domain.csproj", "Zeno.Domain/"]
COPY ["Zeno.Infrastructure.SQL/Zeno.Infrastructure.SQL.csproj", "Zeno.Infrastructure.SQL/"]

RUN dotnet restore "Zeno/Zeno.API.csproj"

COPY . .

WORKDIR "/src/Zeno"
RUN dotnet publish "Zeno.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

CMD ["sh", "-c", "dotnet Zeno.API.dll --urls http://0.0.0.0:${PORT:-8080}"]