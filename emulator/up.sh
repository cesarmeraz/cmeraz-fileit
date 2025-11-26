#!/bin/bash
# import environment variables from .env file
. .env

if [[ -n "$CONFIG_PATH" ]]; then
  echo "CONFIG_PATH is set to '$CONFIG_PATH'."
else
  echo "CONFIG_PATH is unset."
  exit 1
fi

if [[ -n "$ACCEPT_EULA" ]]; then
  echo "ACCEPT_EULA is set to '$ACCEPT_EULA'."
else
  echo "ACCEPT_EULA is unset."
  exit 1
fi

if [[ -n "$SQL_PASSWORD" ]]; then
  echo "SQL_PASSWORD is set to '$SQL_PASSWORD'."
else
  echo "SQL_PASSWORD is unset."
  exit 1
fi

if [[ -n "$SQL_WAIT_INTERVAL" ]]; then
  echo "SQL_WAIT_INTERVAL is set to '$SQL_WAIT_INTERVAL'."
else
  echo "SQL_WAIT_INTERVAL is unset."
  exit 1
fi

docker compose -f ./docker-compose.yaml up -d
exit 0