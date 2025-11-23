@ECHO OFF
TITLE Front-End (Serveur Local)

ECHO Lancement du serveur web Python sur http://localhost:8000

CD /D "%~dp0"

ECHO Le serveur est en cours d'execution dans ce terminal.
ECHO Appuyez sur CTRL+C pour l'arreter.
ECHO.

REM Ouvre le navigateur par defaut sur la bonne page
START http://localhost:8000/homepage/homepage.html

python -m http.server 8000