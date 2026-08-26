@echo off
setlocal

set PG=C:\Program Files\PostgreSQL\16\bin
set DATA=C:\pgweek1data5433
set LOG=%DATA%\startup.log

if not exist "%DATA%\postgresql.conf" (
  echo Initializing PostgreSQL cluster on port 5433...
  "%PG%\initdb.exe" -D "%DATA%" -U postgres --auth=trust -A trust
)

copy "%DATA%\pg_hba.conf" "%DATA%\pg_hba.conf.bak" >nul 2>&1
> "%DATA%\pg_hba.conf" (
  echo local   all             all                                     trust
  echo host    all             all             127.0.0.1/32            trust
  echo host    all             all             ::1/128                 trust
  echo local   replication     all                                     trust
  echo host    replication     all             127.0.0.1/32            trust
  echo host    replication     all             ::1/128                 trust
)

if not exist "%DATA%\postgresql.conf.bak" (
  copy "%DATA%\postgresql.conf" "%DATA%\postgresql.conf.bak" >nul 2>&1
)

powershell -NoProfile -ExecutionPolicy Bypass -Command "(Get-Content '%DATA%\postgresql.conf') -replace '^(#\s*)?port\s*=.*$','port = 5433' -replace '^(#\s*)?listen_addresses\s*=.*$','listen_addresses = ''localhost''' | Set-Content '%DATA%\postgresql.conf'"

if not exist "%LOG%" type nul > "%LOG%"

call "%PG%\pg_ctl.exe" -D "%DATA%" -l "%LOG%" -o "-p 5433 -h localhost" start >nul 2>&1

timeout /t 5 >nul

"%PG%\psql.exe" -p 5433 -h localhost -U postgres -d postgres -c "DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'week1_app') THEN CREATE ROLE week1_app WITH LOGIN PASSWORD 'week1_secret'; END IF; END $$;" >nul 2>&1

for /f %%I in ('"%PG%\psql.exe" -p 5433 -h localhost -U postgres -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = 'week1_rbac';" 2^>nul') do set DBFOUND=%%I
if not "%DBFOUND%"=="1" (
  "%PG%\psql.exe" -p 5433 -h localhost -U postgres -d postgres -c "CREATE DATABASE week1_rbac OWNER week1_app;"
)

"%PG%\psql.exe" -p 5433 -h localhost -U postgres -d postgres -c "SELECT datname FROM pg_database WHERE datname = 'week1_rbac';"

echo.
echo PostgreSQL database ready at localhost:5433
echo Role: week1_app
echo Database: week1_rbac
