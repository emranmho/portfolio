# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG GIT_SHA=dev
WORKDIR /src

COPY apps/api/Portfolio.slnx ./
COPY apps/api/src/Portfolio.Domain/Portfolio.Domain.csproj src/Portfolio.Domain/
COPY apps/api/src/Portfolio.Application/Portfolio.Application.csproj src/Portfolio.Application/
COPY apps/api/src/Portfolio.Infrastructure/Portfolio.Infrastructure.csproj src/Portfolio.Infrastructure/
COPY apps/api/src/Portfolio.Api/Portfolio.Api.csproj src/Portfolio.Api/
RUN dotnet restore src/Portfolio.Api/Portfolio.Api.csproj

COPY apps/api/src/ src/
COPY content/ /content/
RUN dotnet publish src/Portfolio.Api/Portfolio.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
ARG GIT_SHA=dev
ENV GIT_SHA=${GIT_SHA} \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Content__Root=/content \
    ConnectionStrings__Db="Data Source=/data/portfolio.db"

RUN addgroup -S app && adduser -S app -G app \
    && apk add --no-cache wget

WORKDIR /app
COPY --from=build /app ./
COPY --from=build /content/ /content/

RUN mkdir -p /data && chown -R app:app /app /data
USER app

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD wget -qO- http://localhost:8080/api/health || exit 1

ENTRYPOINT ["dotnet", "Portfolio.Api.dll"]
