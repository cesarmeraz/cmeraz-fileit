#!/usr/bin/env bash
set -euo pipefail

# Configurable via env:
HOST="${HOST:-127.0.0.1}"
BLOB_PORT="${BLOB_PORT:-10000}"
AZURITE_CMD="${AZURITE_CMD:-$HOME/azurite/azurite}"
DATA_DIR="${DATA_DIR:-$HOME/azurite/azurite_data}"
LOG_FILE="${LOG_FILE:-$HOME/azurite/azurite.log}"
PID_FILE="${PID_FILE:-$DATA_DIR/azurite.pid}"
START_TIMEOUT="${START_TIMEOUT:-15}"

# Returns 0 if something is listening on $HOST:$BLOB_PORT or azurite process exists
is_azurite_up() {
    # 1) Try HTTP probe
    if command -v curl >/dev/null 2>&1; then
        if curl -s --fail --connect-timeout 2 "http://$HOST:$BLOB_PORT/" >/dev/null 2>&1; then
            return 0
        fi
    fi

    # 2) Try bash /dev/tcp (if supported)
    if (exec 3<>/dev/tcp/"$HOST"/"$BLOB_PORT") >/dev/null 2>&1; then
        exec 3>&- 3<&-
        return 0
    fi

    # 3) Try nc
    if command -v nc >/dev/null 2>&1; then
        if nc -z "$HOST" "$BLOB_PORT" >/dev/null 2>&1; then
            return 0
        fi
    fi

    # 4) Try ss/tcp listing
    if command -v ss >/dev/null 2>&1; then
        if ss -ltn 2>/dev/null | grep -qE "[:.]${BLOB_PORT}(\s|$)"; then
            return 0
        fi
    fi

    # 5) Check for an azurite process that mentions blob port (best-effort)
    if pgrep -f "azurite.*blobPort|azurite" >/dev/null 2>&1; then
        return 0
    fi

    return 1
}

# If already up, report and exit
if is_azurite_up; then
    echo "Azurite blob service appears to be running on $HOST:$BLOB_PORT"
    exit 0
fi

# Ensure azurite command exists
if ! command -v "${AZURITE_CMD%% *}" >/dev/null 2>&1; then
    echo "Error: '$AZURITE_CMD' not found. Install azurite or set AZURITE_CMD to its path." >&2
    exit 1
fi

mkdir -p "$DATA_DIR" "$(dirname "$LOG_FILE")"

# Start only blob service (flags are compatible with azurite v3+). Redirect output to log.
nohup "$AZURITE_CMD" --blobHost "$HOST" --blobPort "$BLOB_PORT" --location "$DATA_DIR" --debug >"$LOG_FILE" 2>&1 &
AZ_PID=$!
echo "$AZ_PID" > "$PID_FILE"

# Wait until service responds or timeout
start_ts=$(date +%s)
while :; do
    if is_azurite_up; then
        echo "Azurite blob service started (pid: $AZ_PID) and is listening on $HOST:$BLOB_PORT"
        exit 0
    fi

    now=$(date +%s)
    if [ $((now - start_ts)) -ge "$START_TIMEOUT" ]; then
        echo "Timed out waiting for Azurite to start after ${START_TIMEOUT}s" >&2
        # If we started a process, try to stop it and cleanup pidfile
        if kill -0 "$AZ_PID" >/dev/null 2>&1; then
            kill "$AZ_PID" >/dev/null 2>&1 || true
            sleep 1
            kill -9 "$AZ_PID" >/dev/null 2>&1 || true
        fi
        rm -f "$PID_FILE" || true
        exit 2
    fi

    sleep 1
done