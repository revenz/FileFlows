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
    echo Preparing Node update files
    
    rem Ensure temporary staging folders are clean
    rmdir /q /s TempNode 2>NUL
    rmdir /q /s TempFlowRunner 2>NUL
    
    rem Handle Node folder
    if exist NodeUpdate\Node\Node (
        robocopy NodeUpdate\Node\Node TempNode /E
    ) else (
        robocopy NodeUpdate\Node TempNode /E
    )
    
    rem Handle FlowRunner folder
    if exist NodeUpdate\FlowRunner\FlowRunner (
        robocopy NodeUpdate\FlowRunner\FlowRunner TempFlowRunner /E
    ) else (
        robocopy NodeUpdate\FlowRunner TempFlowRunner /E
    )
    
    rem Move staged folders into place
    move TempNode Node
    move TempFlowRunner FlowRunner
    
    rem Cleanup
    rmdir /q /s NodeUpdate
    
    echo.
    echo Starting FileFlows Node
    start /D "%~dp0Node" FileFlows.Node.exe
    
    if exist node-upgrade.bat goto Done
    del node-upgrade.bat & exit

:Done
exit
