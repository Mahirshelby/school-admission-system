FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SchoolAdmission.Domain/SchoolAdmission.Domain.csproj SchoolAdmission.Domain/
COPY SchoolAdmission.Application/SchoolAdmission.Application.csproj SchoolAdmission.Application/
COPY SchoolAdmission.Infrastructure/SchoolAdmission.Infrastructure.csproj SchoolAdmission.Infrastructure/
COPY SchoolAdmission.API/SchoolAdmission.API.csproj SchoolAdmission.API/
RUN dotnet restore SchoolAdmission.API/SchoolAdmission.API.csproj

COPY SchoolAdmission.Domain/ SchoolAdmission.Domain/
COPY SchoolAdmission.Application/ SchoolAdmission.Application/
COPY SchoolAdmission.Infrastructure/ SchoolAdmission.Infrastructure/
COPY SchoolAdmission.API/ SchoolAdmission.API/
RUN dotnet publish SchoolAdmission.API/SchoolAdmission.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SchoolAdmission.API.dll"]
