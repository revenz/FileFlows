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
    echo Searching for target directories...
    
    rem Store the current directory
    set "CURRENT_DIR=%cd%"
    
rem Check if the necessary folders exist in the current directory
:CheckFolders
    echo Checking directory: %CURRENT_DIR%
    
    if exist "%CURRENT_DIR%\Node" if exist "%CURRENT_DIR%\FlowRunner" if exist "%CURRENT_DIR%\Logs" (
        echo Found required directories at %CURRENT_DIR%
        goto Found
    )
    
    rem Move up one directory
    cd ..
    set "NEW_DIR=%cd%"
    
    rem If we've reached the root (no change in directory), stop
    if "%NEW_DIR%" == "%CURRENT_DIR%" (
        echo Could not find required directories, extracting to original directory.
        goto Extract
    )
    
    rem Update the current directory and check the next level
    set "CURRENT_DIR=%NEW_DIR%"
    goto CheckFolders
    
:Found
    echo Found required directories at %CURRENT_DIR%
    cd %CURRENT_DIR%
    
    rem Now, proceed with the update process
    echo Removing previous version
    rmdir /q /s "%~dp0Node"
    rmdir /q /s "%~dp0FlowRunner"
    
    echo.
    echo Copying Node update files
    if exist NodeUpdate/FlowRunner/FlowRunner (
        move NodeUpdate/FlowRunner/FlowRunner FlowRunner
    ) else (
        move NodeUpdate/FlowRunner FlowRunner
    )
    
    if exist NodeUpdate/Node/Node (
        move NodeUpdate/Node/Node Node
    ) else ( 
        move NodeUpdate/Node Node
    )
    
    rmdir /q /s "%~dp0%NodeUpdate"
    
    echo.
    echo Starting FileFlows Node
    start /D "%~dp0Node" FileFlows.Node.exe
    
    if exist node-upgrade.bat goto Done
    del node-upgrade.bat & exit
    
:Extract
rem Extracting to the original directory
    echo Extracting update files to original directory...
    move NodeUpdate/FlowRunner FlowRunner
    move NodeUpdate/Node Node
    rmdir /q /s NodeUpdate
    
    echo Starting FileFlows Node
    start /D "%~dp0Node" FileFlows.Node.exe
    
:Done
exit
