#!/bin/bash

# Démarrer le service MariaDB
service mariadb start

# Attendre que le service soit prêt
sleep 20

# Tester la connexion à MariaDB
echo "Testing connection to MariaDB..."
mysql -u root -p"$MYSQL_ROOT_PASSWORD" -e "SHOW DATABASES;" > /dev/null 2>&1
if [ $? -ne 0 ]; then
    echo "Error: Unable to connect to MariaDB with the provided root password."
    exit 1
fi

# Vérifier si la base de données est déjà initialisée
DB_EXISTS=$(mysql -u root -p"$MYSQL_ROOT_PASSWORD" -e "SHOW DATABASES LIKE '${MYSQL_DATABASE}';" | grep "${MYSQL_DATABASE}")

if [ -z "$DB_EXISTS" ]; then
    echo "Initializing MariaDB..."

    # Créer la base de données et l'utilisateur
    mysql -u root -p"$MYSQL_ROOT_PASSWORD" -e "CREATE DATABASE IF NOT EXISTS ${MYSQL_DATABASE};"
    mysql -u root -p"$MYSQL_ROOT_PASSWORD" -e "GRANT ALL PRIVILEGES ON *.* TO 'root'@'localhost' IDENTIFIED BY '${MYSQL_ROOT_PASSWORD}'; FLUSH PRIVILEGES;"
else
    echo "Database '${MYSQL_DATABASE}' already initialized. Skipping initialization."
fi

echo "Generating new migrations..."
dotnet ef migrations add UpdateMigration --project /app/API_Produit.csproj

echo "Running migrations..."
dotnet ef database update --project /app/API_Produit.csproj

# Lancer l'application .NET
echo "Starting the .NET application..."
dotnet API_Produit.dll

