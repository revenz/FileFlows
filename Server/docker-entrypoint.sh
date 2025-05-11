#!/usr/bin/env bash
set -e  # Exit immediately if a command exits with a non-zero status.

# Delete existing log file if it exists
[ -f /app/startup.log ] && rm /app/startup.log

# Start logging to startup.log and also output to console
exec > >(tee -a /app/startup.log) 2>&1

# Function to stop logging to startup.log and continue logging only to the console
stopLogging() {
  exec > /proc/1/fd/1 2>/proc/1/fd/2
}

# Function to handle graceful shutdown
shutdown() {
    echo "Shutting down gracefully..."
    if [[ -n "$dotnet_pid" ]]; then
        kill -TERM "$dotnet_pid"
        wait "$dotnet_pid"
    fi
    exit 0
}

# Trap SIGTERM and SIGINT to ensure a graceful shutdown
trap shutdown SIGTERM SIGINT

dockermods() {

    # Run dpkg to configure any pending package installations
    dpkg --configure -a

    # Directory to search for .sh files
    dirDockerMods="/app/Data/DockerMods"

    # Check if directory exists
    if [ -d "$dirDockerMods" ]; then
        # Change directory to /app/DockerMods and store the previous directory
        pushd "$dirDockerMods" > /dev/null && {
            dotnet /app/FileFlowsLoading/FileFlowsLoading.dll --urls=http://*:5000 >/dev/null 2>&1 &

            # Set execute permission for all .sh files
            chmod +x *.sh
            
            # Create output.log file or clear existing one
            > output.log

            # Find all .sh files and execute them
            for file in *.sh; do

                # Calculate the length of the filename including " DockerMod: "
                length=$(( ${#file} + 13 ))

                # Calculate the number of equals signs needed on each side
                half_length=$(( ( ( 80 - length ) / 2 ) - 1 ))

                # Generate equals signs to match the length of the filename
                equals_left=$(printf "%-${half_length}s" "=" | tr ' ' '=')
                equals_right=$(printf "%-${half_length}s" "=" | tr ' ' '=')

                # Check if the length is odd, if so, add one more equals sign to the left side
                if [ $(( length % 2 )) -eq 1 ]; then
                    equals_left+="="
                fi

                # Add the header line with the " DockerMod: $file " centered
                echo "$equals_left DockerMod: $file $equals_right" | tee -a output.log

                # Capture the start time
                start_time=$(date +%s)

                # Execute the script, append the output to output.log and also display it on the console
                bash "$file" 2>&1 | tee -a output.log

                # Capture the end time
                end_time=$(date +%s)

                # Calculate the difference in seconds
                time_diff=$((end_time - start_time))

                # Convert the difference to minutes and seconds
                minutes=$((time_diff / 60))
                seconds=$((time_diff % 60))

                # Print the time difference in mm:ss format
                echo "Time elapsed: $minutes minutes $seconds seconds"

                # Add the footer line with equals signs matching the length of the header line
                echo "================================================================================" | tee -a output.log

                # Add an empty line
                echo | tee -a output.log

            done

            # Now, kill the process
            echo "Stopping the process..."
            pkill -f dotnet

            # Return to the previous directory
            popd > /dev/null
        } || echo "Failed to change directory to $dirDockerMods"
    fi
}

# Default mode is "server"
mode=server

# Check if running as a node
if [[ "$FFNODE" == 'true' || "$FFNODE" == '1' || "$1" = '--node' ]]; then
    # Check if an upgrade script exists and run it
    if test -f "/app/NodeUpdate/node-upgrade.sh"; then
        printf "Upgrade found\n"
        chmod +x /app/NodeUpdate/node-upgrade.sh
        cd /app/NodeUpdate
        printf "bash node-upgrade.sh docker\n"
        bash node-upgrade.sh docker
    fi
    mode=node
else
    # Check if a server upgrade script exists and run it
    if test -f "/app/Update/server-upgrade.sh"; then
        printf "Upgrade found\n"
        chmod +x /app/Update/server-upgrade.sh
        cd /app/Update
        printf "bash server-upgrade.sh docker\n"
        bash server-upgrade.sh docker
    fi
fi

# Run as root if PUID is not set
if [[ -z "${PUID}" ]]; then
    # Run DockerMods before starting 
    dockermods
    if [[ "$mode" == "node" ]]; then
        printf "Launching node as root\n"
        cd /app/Node
        stopLogging
        dotnet FileFlows.Node.dll --docker true &
        dotnet_pid=$!
    else
        printf "Launching server as root\n"
        cd /app/Server
        stopLogging
        dotnet FileFlows.Server.dll --urls=http://*:5000 --docker &
        dotnet_pid=$!
    fi
else
    # running as PGID/PUID
    pgid=${PGID}
    if [[ -z "${PGID}" ]]; then
        pgid="${PUID}"
    fi
    
    user=fileflows

    # Check if the user exists
    if id "${PUID}" &>/dev/null; then
        printf "${PUID} user exists\n"
        user="$(id -nu "${PUID}")"
    else
        if [ $(getent group $pgid) ]; then
            printf "group $pgid exists\n"
        else
            printf "fileflows group does not exist, creating\n"
            groupadd -g $pgid fileflows
        fi

        printf "user '$user' does not exist, creating\n"
        useradd -u "${PUID}" -g $pgid $user
        if id "${PUID}" &>/dev/null; then
            printf "created user '$user'\n"
        else
            printf "failed to create user '$user'\n"
            exit
        fi
    fi
    
    # Setup permissions for intel
    if [ -e /dev/dri ]; then
        FILES=$(find /dev/dri -type c)

        for i in ${FILES}; do
            VIDEO_GID=$(stat -c '%g' "${i}")
            VIDEO_UID=$(stat -c '%u' "${i}")
            # check if user matches device
            if id -u $user | grep -qw "${VIDEO_UID}"; then
                printf "**** permissions for ${i} are good ****\n"
            else
                # check if group matches and that device has group rw
                if id -G $user | grep -qw "${VIDEO_GID}" && [[ $(stat -c '%A' "${i}" | cut -b 5,6) == "rw" ]]; then
                    printf "**** permissions for ${i} are good ****\n"
                # check if device needs to be added to video group
                elif ! id -G $user | grep -qw "${VIDEO_GID}"; then
                    # check if video group needs to be created
                    VIDEO_NAME=$(getent group "${VIDEO_GID}" | awk -F: '{print $1}')
                    if [[ -z "${VIDEO_NAME}" ]]; then
                        VIDEO_NAME="video$(head /dev/urandom | tr -dc 'a-z0-9' | head -c4)"
                        groupadd "${VIDEO_NAME}"
                        groupmod -g "${VIDEO_GID}" "${VIDEO_NAME}"
                        printf "**** creating video group ${VIDEO_NAME} with id ${VIDEO_GID} ****\n"
                    fi
                    printf "**** adding ${i} to video group ${VIDEO_NAME} with id ${VIDEO_GID} ****\n"
                    usermod -a -G "${VIDEO_NAME}" "${user}"
                fi
                # check if device has group rw
                if [[ $(stat -c '%A' "${i}" | cut -b 5,6) != "rw" ]]; then
                    printf "**** The device ${i} does not have group read/write permissions, attempting to fix inside the container. ****\n"
                    chmod g+rw "${i}"
                fi
            fi
        done
    fi       
    
    # Add user to additional groups if specified
    if [[ -n "${PGIDS}" ]]; then
        IFS=';' read -ra ADDITIONAL_GROUPS <<< "${PGIDS}"
        for group in "${ADDITIONAL_GROUPS[@]}"; do
            group_name=$(getent group "$group" | cut -d: -f1)
            if [[ -z "$group_name" ]]; then
                printf "Group with GID $group does not exist\n"
                continue
            fi
            if getent group "$group_name" &>/dev/null; then
                printf "Adding user to group $group_name\n"
                usermod -aG "$group_name" "$user"
            else
                printf "Group $group_name does not exist, skipping\n"
            fi
        done
    fi

    printf "Changing ownership of /app to: ${PUID}:$pgid\n"
    chown -R "${PUID}:$pgid" /app
    passwd -d root

    # Run DockerMods before starting
    dockermods
    if [[ "$mode" == "node" ]]; then
        printf "Launching node as '$user'\n"
        cd /app/Node
        stopLogging
        su -c "/dotnet/dotnet FileFlows.Node.dll --docker true" "$user" &
        dotnet_pid=$!
    else
        printf "Launching server as '$user'\n"
        cd /app/Server
        stopLogging
        su -c "/dotnet/dotnet FileFlows.Server.dll --urls=http://*:5000 --docker" "$user" &
        dotnet_pid=$!
    fi
fi

# Wait for the process to exit, ensuring the script doesn't terminate prematurely
wait "$dotnet_pid"
