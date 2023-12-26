# This dockerfile is a combination of two things:
# * A dotnet build of a csproj assumed to use the SqlProj SDK found here - https://github.com/rr-wfm/MSBuild.Sdk.SqlProj/
# * Building on a SQL Server image to deploy a DACPAC, adapted from https://www.wintellect.com/devops-sql-server-dacpac-docker/

# First, the dotnet build of the DACPAC. The csproj referenced here should be
# one that uses this SDK: https://github.com/rr-wfm/MSBuild.Sdk.SqlProj/
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR .
COPY ["/src/TaleCodeDatabase/TaleCodeDatabase.csproj", "TaleCodeDatabase/"]
RUN dotnet restore "TaleCodeDatabase/TaleCodeDatabase.csproj"
COPY . .
WORKDIR "/src/TaleCodeDatabase"
RUN dotnet build "TaleCodeDatabase.csproj" -c Release -o /app/build

# Then we create the final image using instructions adapted from https://www.wintellect.com/devops-sql-server-dacpac-docker/
FROM mcr.microsoft.com/mssql/server:2019-latest AS final

USER root

### Install Unzip
RUN apt-get update \
    && apt-get install unzip -y

# Install SQLPackage for Linux and make it executable
RUN wget -progress=bar:force -q -O sqlpackage.zip https://aka.ms/sqlpackage-linux \
    && unzip -qq sqlpackage.zip -d /opt/sqlpackage \
    && chmod +x /opt/sqlpackage/sqlpackage \
    && chown -R mssql /opt/sqlpackage \
    && mkdir /tmp/db \
    && chown -R mssql /tmp/db

### Add the DACPAC to the image
COPY --from=build /app/build/TaleCodeDatabase.dacpac /tmp/db/db.dacpac

### Configure external build arguments to allow configurability.
ARG DBNAME=TaleCodeDatabase
ARG PASSWORD=password_xxddd_2137

### Configure the required environmental variables
ENV ACCEPT_EULA=Y
ENV SA_PASSWORD=$PASSWORD

### Launch SQL Server, confirm startup is complete, deploy the DACPAC, then delete the DACPAC and terminate SQL Server.
### See https://stackoverflow.com/a/51589787/488695
RUN ( /opt/mssql/bin/sqlservr & ) | grep -q "Service Broker manager has started" \
    && /opt/sqlpackage/sqlpackage /a:Publish /tsn:localhost /tdn:${DBNAME} /tu:sa /tp:$SA_PASSWORD /ttsc:true /sec:false /tec:false /sf:/tmp/db/db.dacpac \
    && rm -r /tmp/db \
    && pkill sqlservr \
    && rm -r /opt/sqlpackage