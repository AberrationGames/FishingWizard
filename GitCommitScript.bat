set /P _inputname= Please enter commit message:
git add .
git commit -m "%_inputname%"
git push
pause