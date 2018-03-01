FROM microsoft/dotnet:2.1-sdk AS build-env
WORKDIR /app

# copy minimum and restore
COPY NuGet.config ./
COPY *.sln ./
COPY InMemoryTransport/*.csproj ./InMemoryTransport/
RUN dotnet restore

# copy everything else and build
COPY . ./
RUN dotnet publish -c Release -f netcoreapp2.1 -o out

# build runtime image
FROM microsoft/dotnet:2.1-runtime
WORKDIR /app
COPY --from=build-env /app/InMemoryTransport/out ./
ENTRYPOINT ["dotnet", "InMemoryTransport.dll"]
