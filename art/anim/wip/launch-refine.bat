@echo off
setlocal
cd /d C:\Users\stuwj\Documents\Coding\ror2-lightning
if not exist art\anim\wip mkdir art\anim\wip
echo ===== refine launch %DATE% %TIME% item1 shoulder =====>> art\anim\wip\refine-run-log.txt
set AGENT=C:\Users\stuwj\AppData\Local\cursor-agent\agent.cmd
REM Read prompt file into a var is hard in bat for multiline; use agent with @file if supported, else python launcher.
python art\anim\wip\_launch_refine.py
echo ===== refine wrapper exit %ERRORLEVEL% %DATE% %TIME% =====>> art\anim\wip\refine-run-log.txt
