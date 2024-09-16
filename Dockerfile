FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app

RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet build -c Release -o app/out
RUN dotnet publish -c Release -o /app/out

RUN apt-get update && apt-get install -y mariadb-server

ENV MYSQL_ROOT_PASSWORD=zDH-.42l3lSRIT1p
ENV MYSQL_DATABASE=produits_db
ENV MYSQL_USER=client_user
ENV MYSQL_PASSWORD=azerty

WORKDIR /app/out

COPY init-mariadb.sh /usr/local/bin/init-mariadb.sh
RUN chmod +x /usr/local/bin/init-mariadb.sh

EXPOSE 3306 5057

CMD ["init-mariadb.sh"]
