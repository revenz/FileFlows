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
    
    rem Ensure the destination directories exist
    echo Ensuring Node and FlowRunner directories exist...
    if not exist "%~dp0Node" mkdir "%~dp0Node"
    if not exist "%~dp0FlowRunner" mkdir "%~dp0FlowRunner"
    
    rem Empty the Node and FlowRunner directories, without deleting the directories
    echo Emptying Node directory...
    del /q "%~dp0Node\*" 2>NUL
    
    echo Emptying FlowRunner directory...
    del /q "%~dp0FlowRunner\*" 2>NUL
    
    echo.
    echo Moving Node update files
    rem Move the entire NodeUpdate/Node directory content (including subdirectories)
    if exist NodeUpdate/Node\* (
        move /Y NodeUpdate/Node\* "%~dp0Node\"
    ) else (
        move /Y NodeUpdate/Node "%~dp0Node"
    )
    
    rem Move the entire NodeUpdate/FlowRunner directory content (including subdirectories)
    if exist NodeUpdate/FlowRunner\* (
        move /Y NodeUpdate/FlowRunner\* "%~dp0FlowRunner\"
    ) else (
        move /Y NodeUpdate/FlowRunner "%~dp0FlowRunner"
    )
    
    rem Delete NodeUpdate directory after moving the files
    rmdir /q /s "%~dp0%NodeUpdate"
    
    echo.
    echo Starting FileFlows Node
    start /D "%~dp0Node" FileFlows.Node.exe
    
    if exist node-upgrade.bat goto Done
    del node-upgrade.bat & exit
    
:Extract
    rem Extracting to the original directory
    echo Extracting update files to original directory...
    move /Y NodeUpdate/FlowRunner "%~dp0FlowRunner"
    move /Y NodeUpdate/Node "%~dp0Node"
    
    rem Delete NodeUpdate directory after extraction
    rmdir /q /s NodeUpdate
    
    echo Starting FileFlows Node
    start /D "%~dp0Node" FileFlows.Node.exe
    
:Done
exit
