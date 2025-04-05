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
    rmdir /q /s "%~dp0%Node"
    rmdir /q /s "%~dp0%FlowRunner"
    
    echo.
    echo Fixing double-nested Node or FlowRunner folders if present
    rem Flatten Node/Node to just Node
    if exist NodeUpdate\Node\Node (
        move NodeUpdate\Node\Node NodeUpdate\TempNode
        rmdir /q /s NodeUpdate\Node
        move NodeUpdate\TempNode NodeUpdate\Node
    )
    
    rem Flatten FlowRunner/FlowRunner to just FlowRunner
    if exist NodeUpdate\FlowRunner\FlowRunner (
        move NodeUpdate\FlowRunner\FlowRunner NodeUpdate\TempFlowRunner
        rmdir /q /s NodeUpdate\FlowRunner
        move NodeUpdate\TempFlowRunner NodeUpdate\FlowRunner
    )
    
    echo.
    echo Copying update files
    move NodeUpdate\Node Node
    move NodeUpdate\FlowRunner FlowRunner
    
    rmdir /q /s NodeUpdate
    
    echo.
    echo Starting FileFlows Node
    start /D "%~dp0Node" FileFlows.Node.exe
    
    if exist node-upgrade.bat goto Done
    del node-upgrade.bat & exit
    
:Done
exit
