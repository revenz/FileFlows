@echo off

IF "%1" == "UPDATE" GOTO RunUpdate
    echo Launching from Subdirectory %1
    copy node-upgrade.bat ..\node-upgrade.bat
    start /D "..\" node-upgrade.bat UPDATE %1 & exit
GOTO Done


:RunUpdate
            
    echo Running Update
    timeout /t 3
    echo Stopping FileFlows Node if running
    taskkill /PID %2 2>NUL
    
    echo.
    echo Removing previous version
    rmdir /q /s "%~dp0Node"
    rmdir /q /s "%~dp0FlowRunner"
    
    echo.
    echo Detecting nested folders
    
    set "NODE_SOURCE=NodeUpdate\Node"
    if exist NodeUpdate\Node\Node (
        set "NODE_SOURCE=NodeUpdate\Node\Node"
    )
    
    set "FLOWRUNNER_SOURCE=NodeUpdate\FlowRunner"
    if exist NodeUpdate\FlowRunner\FlowRunner (
        set "FLOWRUNNER_SOURCE=NodeUpdate\FlowRunner\FlowRunner"
    )
    
    echo.
    echo Moving update files
    move "%NODE_SOURCE%" Node
    move "%FLOWRUNNER_SOURCE%" FlowRunner
    
    echo.
    echo Cleaning up
    rmdir /q /s NodeUpdate
    
    echo.
    echo Starting FileFlows Node
    start /D "%~dp0Node" FileFlows.Node.exe
    
    if exist node-upgrade.bat goto Done
    del node-upgrade.bat & exit

:Done
exit
