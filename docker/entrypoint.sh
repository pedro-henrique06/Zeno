#!/bin/bash
/opt/mssql/bin/sqlservr &

echo "Aguardando SQL Server iniciar..."
until /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -C -Q "SELECT 1" > /dev/null 2>&1; do
  sleep 2
done

echo "Executando init.sql..."
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -C -i /tmp/init.sql

echo "Banco inicializado."
wait
