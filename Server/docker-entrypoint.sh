#!/usr/bin/env bash

# set -e makes the script exit when a command fails.
set -e

# Start logging to startup.log and also output to console
exec > >(tee -a /app/startup.log) 2>&1

stopLogging() {
  # Stop logging to startup.log and continue logging only to the console
  exec > /proc/1/fd/1 2>/proc/1/fd/2
}

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

mode=server

if [[ "$FFNODE" == 'true' || "$FFNODE" == '1' || "$1" = '--node' ]]; then

    # check if there is an upgrade to apply
    if test -f "/app/NodeUpdate/node-upgrade.sh"; then
        printf "Upgrade found\n"
        chmod +x /app/NodeUpdate/node-upgrade.sh
        cd /app/NodeUpdate
        printf "bash node-upgrade.sh docker\n"
        bash node-upgrade.sh docker
    fi

    mode=node
    
else

    # check if there is an upgrade to apply
    if test -f "/app/Update/server-upgrade.sh"; then
        printf "Upgrade found\n"
        chmod +x /app/Update/server-upgrade.sh
        cd /app/Update
        printf "bash server-upgrade.sh docker\n"
        bash server-upgrade.sh docker
    fi
fi



if [[ -z "${PUID}" ]]; then

    if [[ "$mode" == "node" ]]; then

        printf "Launching node as root\n"
        cd /app/Node
        stopLogging
        exec /dotnet/dotnet FileFlows.Node.dll --docker true

    else

        # dockermods

        printf "Launching server as root\n"
        cd /app/Server
        stopLogging
        exec /dotnet/dotnet FileFlows.Server.dll --urls=http://*:5000 --docker

    fi

else 
    # running as PGID/PUID
    pgid=${PGID}
    if [[ -z "${PGID}" ]]; then
        pgid="${PUID}"
    fi

    user=fileflows

    if id "${PUID}" &>/dev/null; then
        printf "${PUID} user exists\n"
        user="$(id -u -n ${PUID})"
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
        
    # Handle additional groups from PGIDS
    if [[ -n "${PGIDS}" ]]; then
        IFS=';' read -ra ADDITIONAL_GROUPS <<< "${PGIDS}"
        for group in "${ADDITIONAL_GROUPS[@]}"; do
            # Resolve group name to GID if it's a number
            if [[ "$group" =~ ^[0-9]+$ ]]; then
                group_name=$(getent group "$group" | cut -d: -f1)
                if [[ -z "$group_name" ]]; then
                    printf "Group with GID $group does not exist\n"
                    continue
                fi
            else
                group_name="$group"
            fi
    
            # Check if the group exists
            if getent group "$group_name" &>/dev/null; then
                printf "Group $group_name exists, adding user to this group\n"
                usermod -aG "$group_name" "$user"
            else
                printf "Group $group_name does not exist, skipping\n"
            fi
        done
    fi

    printf "changing owner of /app to: ${PUID}:$pgid\n"
    chown -R "${PUID}:$pgid" /app
    printf "changed owner of /app to: ${PUID}:$pgid\n"

    # need to clear password for root, this is so DockerMods can be run
    passwd -d root

    if [[ "$mode" == "node" ]]; then
        printf "Launching node as '$user'\n"
        cd /app/Node
        stopLogging
        su -c "/dotnet/dotnet FileFlows.Node.dll --docker true" $user

    else

        # dockermods

        printf "Launching server as '$user'\n"
        cd /app/Server
        stopLogging
        su -c "/dotnet/dotnet FileFlows.Server.dll --urls=http://*:5000 --docker" $user
    fi
fi
