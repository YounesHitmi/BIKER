@echo off
title Serveur Frontend (Python)

echo Lancement du serveur web Python...

:: Se place dans le dossier ou se trouve ce script
cd /d "%~dp0"

echo.
echo Le serveur est en cours d'execution.
echo Ouvrez votre navigateur et allez a :
echo.
echo http://localhost:8000/homepage/homepage.html
echo.
echo CETTE FENETRE DOIT RESTER OUVERTE.
echo Appuyez sur Ctrl+C pour arreter le serveur.
echo.

python -m http.server 8000

:: Si Python 3 n'est pas trouve, essaie avec Python 2
python -m SimpleHTTPServer 8000

pause