FROM bigbang/aspnet:8.0 AS base


WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM bigbang/sdk:8.0 AS build


COPY ./.build/NuGet.Config /root/.nuget/NuGet/NuGet.Config
#Add dotin certificate
COPY ./.build/Dotin.crt /usr/local/share/ca-certificates/
COPY ./.build/CertumTrustedNetworkCA.crt /usr/local/share/ca-certificates/
COPY ./.build/CertumDomainValidationCASHA2.crt /usr/local/share/ca-certificates/
RUN update-ca-certificates


#Installing bigbang tool
RUN dotnet tool install -g BigBang.Tool --prerelease --no-cache
ENV PATH="/root/.dotnet/tools:${PATH}"

WORKDIR /src
COPY ["./src/Host/Host.csproj", "./src/Host/"]
COPY ["./src/BigBang.App.Cloud.ERP.Accounting/BigBang.App.Cloud.ERP.Accounting.csproj", "./src/BigBang.App.Cloud.ERP.Accounting/"]
COPY ["./src/BigBang.App.Cloud.ERP.Accounting.DML/BigBang.App.Cloud.ERP.Accounting.DML.csproj", "./src/BigBang.App.Cloud.ERP.Accounting.DML/"]
COPY ["./src/BigBang.App.Cloud.ERP.Accounting.Migrations/BigBang.App.Cloud.ERP.Accounting.Migrations.csproj", "./src/BigBang.App.Cloud.ERP.Accounting.Migrations/"]
COPY ["./Directory.Build.props", "./Directory.Build.props"]

RUN dotnet restore "./src/Host/Host.csproj"
#Copy all sources
COPY . .

#Generate domain
RUN bigbang domain

FROM build AS publish
ARG PRODUCT_VERSION=0.0.0.0

RUN dotnet publish "./src/Host/Host.csproj" -c Release -o /app/publish /property:Version=$PRODUCT_VERSION

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

#Keep entry point bash and change command to dotnet
CMD ["dotnet", "Host.dll"]