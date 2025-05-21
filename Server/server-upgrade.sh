#!/usr/bin/env bash

if [ "$1" == "mac" ]; then
  # this means it running as a mac .app and we treat it differently
  kill "$3"
  
  cd ..
  
  rm -rf upgrade.log
  
  echo 'Upgrading FileFlows' >> upgrade.log
  
  echo "--------------------------------------------" > upgrade.log
  for arg in "$@"; do
      echo "$arg" >> upgrade.log
  done
  echo "--------------------------------------------" > upgrade.log
  
  # update version file
  echo "$3" > version.txt
  
  rm -rf Server >> upgrade.log
  rm -rf FlowRunner >> upgrade.log
  
  mv Update/FlowRunner FlowRunner >> upgrade.log
  mv Update/Server Server >> upgrade.log
  
  echo 'Removing Update folder' >> upgrade.log
  rm -rf Update >> upgrade.log
  
  echo "Launching open -a ""$2""" >> upgrade.log
  
  # Relaunch the macOS .app folder
  # Schedule the application launch using the 'at' command
  (sleep 5 && open -ga "$2") &

  # Exit the script
  exit 0
  
else
  
  if [[ "$1" != "systemd" && "$1" != "docker" ]]; then 
    kill %1
  fi
  
  echo 'Upgrading FileFlows Standard' >> upgrade.log
  
  echo "--------------------------------------------" > version
  for arg in "$@"; do
      echo "$arg" >> upgrade.log
  done
  echo "--------------------------------------------" > version
  
  cd ..
  rm -rf Server
  rm -rf FlowRunner
  rm run-node.sh
  rm run-server.sh
  
  mv Update/FlowRunner FlowRunner
  mv Update/Server Server
  mv Update/run-node.sh run-node.sh
  mv Update/run-server.sh run-server.sh
  
  echo 'Removing Update folder' >> upgrade.log
  rm -rf Update
  
  if [[ "$1" != "systemd" && "$1" != "docker" ]]; then 
    chmod +x run-server.sh
    ./run-server.sh
  fi
fi