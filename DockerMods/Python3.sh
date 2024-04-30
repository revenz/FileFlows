# ----------------------------------------------------------------------------------------------------
# Name: Python3
# Description: Python 3 is a high-level, interpreted programming language known for its simplicity, readability, and versatility, widely used for web development, data analysis, artificial intelligence, and scripting tasks.
# Author: John Andrews
# Revision: 1
# Icon: data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciICB2aWV3Qm94PSIwIDAgNDggNDgiIHdpZHRoPSI0OHB4IiBoZWlnaHQ9IjQ4cHgiPjxwYXRoIGZpbGw9IiMwMjc3QkQiIGQ9Ik0yNC4wNDcsNWMtMS41NTUsMC4wMDUtMi42MzMsMC4xNDItMy45MzYsMC4zNjdjLTMuODQ4LDAuNjctNC41NDksMi4wNzctNC41NDksNC42N1YxNGg5djJIMTUuMjJoLTQuMzVjLTIuNjM2LDAtNC45NDMsMS4yNDItNS42NzQsNC4yMTljLTAuODI2LDMuNDE3LTAuODYzLDUuNTU3LDAsOS4xMjVDNS44NTEsMzIuMDA1LDcuMjk0LDM0LDkuOTMxLDM0aDMuNjMydi01LjEwNGMwLTIuOTY2LDIuNjg2LTUuODk2LDUuNzY0LTUuODk2aDcuMjM2YzIuNTIzLDAsNS0xLjg2Miw1LTQuMzc3di04LjU4NmMwLTIuNDM5LTEuNzU5LTQuMjYzLTQuMjE4LTQuNjcyQzI3LjQwNiw1LjM1OSwyNS41ODksNC45OTQsMjQuMDQ3LDV6IE0xOS4wNjMsOWMwLjgyMSwwLDEuNSwwLjY3NywxLjUsMS41MDJjMCwwLjgzMy0wLjY3OSwxLjQ5OC0xLjUsMS40OThjLTAuODM3LDAtMS41LTAuNjY0LTEuNS0xLjQ5OEMxNy41NjMsOS42OCwxOC4yMjYsOSwxOS4wNjMsOXoiLz48cGF0aCBmaWxsPSIjRkZDMTA3IiBkPSJNMjMuMDc4LDQzYzEuNTU1LTAuMDA1LDIuNjMzLTAuMTQyLDMuOTM2LTAuMzY3YzMuODQ4LTAuNjcsNC41NDktMi4wNzcsNC41NDktNC42N1YzNGgtOXYtMmg5LjM0M2g0LjM1YzIuNjM2LDAsNC45NDMtMS4yNDIsNS42NzQtNC4yMTljMC44MjYtMy40MTcsMC44NjMtNS41NTcsMC05LjEyNUM0MS4yNzQsMTUuOTk1LDM5LjgzMSwxNCwzNy4xOTQsMTRoLTMuNjMydjUuMTA0YzAsMi45NjYtMi42ODYsNS44OTYtNS43NjQsNS44OTZoLTcuMjM2Yy0yLjUyMywwLTUsMS44NjItNSw0LjM3N3Y4LjU4NmMwLDIuNDM5LDEuNzU5LDQuMjYzLDQuMjE4LDQuNjcyQzE5LjcxOSw0Mi42NDEsMjEuNTM2LDQzLjAwNiwyMy4wNzgsNDN6IE0yOC4wNjMsMzljLTAuODIxLDAtMS41LTAuNjc3LTEuNS0xLjUwMmMwLTAuODMzLDAuNjc5LTEuNDk4LDEuNS0xLjQ5OGMwLjgzNywwLDEuNSwwLjY2NCwxLjUsMS40OThDMjkuNTYzLDM4LjMyLDI4Ljg5OSwzOSwyOC4wNjMsMzl6Ii8+PC9zdmc+
# ----------------------------------------------------------------------------------------------------

#!/bin/bash

# Check if Python 3 is installed
if command -v python3 &>/dev/null; then
    echo "Python 3 is already installed."
else
    echo "Python 3 is not installed. Installing..."
    sudo apt update
    sudo apt install -y python3
    echo "Python 3 has been installed."
fi