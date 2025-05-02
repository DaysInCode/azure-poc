#!/bin/sh
set -e

until /opt/mssql-tools/bin/sqlcmd -S sqledge -U sa -P "$MSSQL_SA_PASSWORD" -Q "SELECT 1" > /dev/null 2>&1; do
  echo "Waiting for SQL Edge to be ready..."
  sleep 5
done

echo "SQL Edge is ready. Starting Service Bus Emulator..."
exec "$@"
