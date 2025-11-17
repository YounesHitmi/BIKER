@ECHO OFF
TITLE Lancement des serveurs

ECHO Lancement du ProxyCacheServer (SOAP)...
START "ProxyCacheServer (SOAP)" "%~dp0LetsGoBiking\ProxyCacheServ\bin\Debug\ProxyCacheServ.exe"

ECHO Lancement du RoutingServer (REST)...
START "RoutingServer (REST)" "%~dp0LetsGoBiking\RoutingServer\bin\Debug\RoutingServer.exe"

ECHO Demarrage en cours...