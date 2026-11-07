#!/usr/bin/env bash
set -euo pipefail

# Confirm the actual public API has initialized and seeded its database.
# A mere HTTP 200 from the MVC homepage is insufficient: it can render an API error.
api_ready=false
web_ready=false
for attempt in $(seq 1 100); do
  if curl --fail --silent --show-error --max-time 4 http://localhost:5080/api/events -o /tmp/raceday-ci-events.json 2>/dev/null; then
    if grep -q 'Midrand Spring Run' /tmp/raceday-ci-events.json; then
      api_ready=true
      break
    fi
  fi
  if (( attempt % 10 == 0 )); then
    echo "Waiting for seeded RaceDay API + SQL... attempt ${attempt}/100"
    docker compose ps
  fi
  sleep 3
done
if [[ "$api_ready" != true ]]; then
  echo 'FAIL: API did not return its seeded event data in time.' >&2
  exit 1
fi
echo 'PASS: API returned seeded event data from SQL Server.'

for attempt in $(seq 1 40); do
  if curl --fail --silent --show-error --max-time 4 http://localhost:5180/ -o /tmp/raceday-ci-home.html 2>/dev/null; then
    if grep -q 'Midrand Spring Run' /tmp/raceday-ci-home.html; then
      web_ready=true
      break
    fi
  fi
  if (( attempt % 10 == 0 )); then
    echo "Waiting for MVC event cards... attempt ${attempt}/40"
  fi
  sleep 3
done
if [[ "$web_ready" != true ]]; then
  echo 'FAIL: MVC did not display the event it fetched from the API.' >&2
  exit 1
fi
echo 'PASS: MVC rendered an event from the API; API queried SQL Server.'
echo 'NOTE: This does not test Azure uploads, login, or every Organiser/Participant feature.'
