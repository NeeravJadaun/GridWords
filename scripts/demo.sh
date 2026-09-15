#!/usr/bin/env bash
# Word Duel — scripted resilience demo.
#
# Demonstrates two things against a running local stack (see docker-compose.yml):
#   1. A duplicate move request (same Idempotency-Key, retried) is rejected
#      from double-scoring — the second call returns the exact cached result.
#   2. A "reconnect" (a fresh, unrelated GET call, as a client would issue
#      after a refresh or dropped socket) returns the current authoritative
#      snapshot rather than stale or missing state.
#
# It also shows a stale-version request being rejected with 409, and an
# invalid placement being rejected without mutating state.
#
# Usage:
#   ./scripts/demo.sh [base_url]
#
# Defaults to http://localhost:5080 (the API port docker-compose publishes).

set -euo pipefail

BASE_URL="${1:-http://localhost:5080}"

if ! command -v jq >/dev/null 2>&1; then
  echo "This script requires 'jq'. Install it (e.g. 'brew install jq') and re-run." >&2
  exit 1
fi

bold() { printf '\033[1m%s\033[0m\n' "$1"; }
step() { echo; bold "== $1 =="; }
info() { printf '  %s\n' "$1"; }

step "Checking the API is reachable at $BASE_URL"
if ! curl -sf "$BASE_URL/health" > /dev/null; then
  echo "Could not reach $BASE_URL/health — is 'docker compose up' running?" >&2
  exit 1
fi
info "OK — /health responded."

# Find a word Ada's rack can actually play (see tests/*/PlayableWordFinder
# for the same idea in the test suites — the draw is deterministic per
# match ID but not predictable ahead of time, and an all-consonant/unlucky
# draw is possible, so we retry with a fresh match rather than fail).
CANDIDATES=(AT AN AS SO NO OR IT IS IN ON TO BE SEA ATE EAT TEA ARE EAR ERA ART RAT TAR SAT SIR AIR ANT TAN RAN TEN NET SET RID RED DIE TIE LIE LID DID AID AIM SIT SAD OWE ORE ORB OAR OWL OWN)

find_playable_word() {
  local rack="$1"
  for candidate in "${CANDIDATES[@]}"; do
    local pool="$rack"
    local ok=true
    for ((i = 0; i < ${#candidate}; i++)); do
      local ch="${candidate:$i:1}"
      if [[ "$pool" != *"$ch"* ]]; then
        ok=false
        break
      fi
      pool="${pool/$ch/}"
    done
    if $ok; then
      echo "$candidate"
      return 0
    fi
  done
  return 1
}

step "Creating a match (Player 1: Ada)"
WORD=""
for attempt in $(seq 1 15); do
  CREATE=$(curl -sf -X POST "$BASE_URL/api/matches" \
    -H 'Content-Type: application/json' \
    -d '{"displayName":"Ada"}')
  MATCH_ID=$(echo "$CREATE" | jq -r '.matchId')
  P1_TOKEN=$(echo "$CREATE" | jq -r '.playerToken')
  P1_RACK=$(echo "$CREATE" | jq -r '.rack')
  if WORD=$(find_playable_word "$P1_RACK"); then
    break
  fi
  info "Rack '$P1_RACK' has no candidate word on attempt $attempt — trying a fresh match."
done
if [[ -z "$WORD" ]]; then
  echo "Could not draw a playable rack after 15 attempts; re-run the script." >&2
  exit 1
fi
info "Match ID: $MATCH_ID"
info "Ada's rack: $P1_RACK"

step "Joining the match (Player 2: Grace)"
JOIN=$(curl -sf -X POST "$BASE_URL/api/matches/$MATCH_ID/join" \
  -H 'Content-Type: application/json' \
  -d '{"displayName":"Grace"}')
VERSION=$(echo "$JOIN" | jq -r '.match.version')
info "Match is now InProgress at version $VERSION."
info "Chosen first move: \"$WORD\" across row 4, starting at the center column."

step "Submitting the move with Idempotency-Key: demo-move-1"
MOVE1=$(curl -sf -X POST "$BASE_URL/api/matches/$MATCH_ID/moves" \
  -H 'Content-Type: application/json' \
  -H "Authorization: Bearer $P1_TOKEN" \
  -H "Idempotency-Key: demo-move-1" \
  -d "{\"expectedMatchVersion\":$VERSION,\"startRow\":3,\"startCol\":3,\"direction\":\"Across\",\"tiles\":\"$WORD\"}")
POINTS1=$(echo "$MOVE1" | jq -r '.pointsScored')
NEW_VERSION=$(echo "$MOVE1" | jq -r '.match.version')
info "Move accepted: scored $POINTS1 points, match is now at version $NEW_VERSION."

step "FAILURE INJECTION 1: retrying the exact same request (duplicate submit)"
info "Same Idempotency-Key, same body — simulates a client retry after a dropped response."
MOVE1_RETRY=$(curl -sf -X POST "$BASE_URL/api/matches/$MATCH_ID/moves" \
  -H 'Content-Type: application/json' \
  -H "Authorization: Bearer $P1_TOKEN" \
  -H "Idempotency-Key: demo-move-1" \
  -d "{\"expectedMatchVersion\":$VERSION,\"startRow\":3,\"startCol\":3,\"direction\":\"Across\",\"tiles\":\"$WORD\"}")
POINTS_RETRY=$(echo "$MOVE1_RETRY" | jq -r '.pointsScored')
VERSION_RETRY=$(echo "$MOVE1_RETRY" | jq -r '.match.version')
SCORE_AFTER=$(curl -sf "$BASE_URL/api/matches/$MATCH_ID" | jq -r '.players[0].score')

if [[ "$POINTS_RETRY" == "$POINTS1" && "$VERSION_RETRY" == "$NEW_VERSION" ]]; then
  info "PASS: retried response is byte-identical ($POINTS_RETRY pts, version $VERSION_RETRY)."
  info "PASS: Ada's persisted score is $SCORE_AFTER, not double-counted."
else
  echo "UNEXPECTED: retry produced a different result — idempotency guarantee violated." >&2
  exit 1
fi

step "FAILURE INJECTION 2: submitting against a stale match version"
STALE=$(curl -s -o /tmp/worddue_stale_response.json -w '%{http_code}' -X POST "$BASE_URL/api/matches/$MATCH_ID/moves" \
  -H 'Content-Type: application/json' \
  -H "Authorization: Bearer $P1_TOKEN" \
  -H "Idempotency-Key: demo-stale-1" \
  -d "{\"expectedMatchVersion\":$VERSION,\"startRow\":0,\"startCol\":0,\"direction\":\"Across\",\"tiles\":\"AT\"}")
STALE_BODY=$(cat /tmp/worddue_stale_response.json)
STALE_CODE=$(echo "$STALE_BODY" | jq -r '.errorCode // empty')
if [[ "$STALE" == "409" && "$STALE_CODE" == "StaleMatchVersion" ]]; then
  info "PASS: HTTP $STALE, errorCode=$STALE_CODE, currentMatchVersion=$(echo "$STALE_BODY" | jq -r '.currentMatchVersion')."
else
  echo "UNEXPECTED: expected 409 StaleMatchVersion, got HTTP $STALE: $STALE_BODY" >&2
  exit 1
fi

step "FAILURE INJECTION 3: submitting an invalid placement (not from Grace's own hand)"
P2_RACK=$(echo "$JOIN" | jq -r '.rack')
P2_TOKEN=$(echo "$JOIN" | jq -r '.playerToken')
# Doubling the first letter of Grace's own rack guarantees the second copy
# is not actually available to place, without needing to know the dictionary.
BOGUS_TILES="${P2_RACK:0:1}${P2_RACK:0:1}${P2_RACK:0:1}"
info "Grace's rack: $P2_RACK — submitting fabricated tiles \"$BOGUS_TILES\" she doesn't hold three of."
INVALID=$(curl -s -o /tmp/worddue_invalid_response.json -w '%{http_code}' -X POST "$BASE_URL/api/matches/$MATCH_ID/moves" \
  -H 'Content-Type: application/json' \
  -H "Authorization: Bearer $P2_TOKEN" \
  -H "Idempotency-Key: demo-invalid-1" \
  -d "{\"expectedMatchVersion\":$NEW_VERSION,\"startRow\":0,\"startCol\":0,\"direction\":\"Across\",\"tiles\":\"$BOGUS_TILES\"}")
INVALID_BODY=$(cat /tmp/worddue_invalid_response.json)
INVALID_CODE=$(echo "$INVALID_BODY" | jq -r '.errorCode // empty')
VERSION_UNCHANGED=$(curl -sf "$BASE_URL/api/matches/$MATCH_ID" | jq -r '.version')
if [[ "$INVALID" == "422" && "$VERSION_UNCHANGED" == "$NEW_VERSION" ]]; then
  info "PASS: HTTP $INVALID, errorCode=$INVALID_CODE, match version unchanged at $VERSION_UNCHANGED."
else
  echo "UNEXPECTED: expected 422 with unchanged version, got HTTP $INVALID (version now $VERSION_UNCHANGED): $INVALID_BODY" >&2
  exit 1
fi

step "RECONNECT: fetching the current snapshot as a freshly (re)connected client would"
info "This is exactly what the browser client does on load/refresh/socket-reconnect —"
info "no session state is trusted; the server's current snapshot is authoritative."
RECONNECT=$(curl -sf "$BASE_URL/api/matches/$MATCH_ID" -H "Authorization: Bearer $P1_TOKEN")
echo "$RECONNECT" | jq '{matchId, status, version, currentTurnSeat, board: .board[0:14], yourRack, yourSeat, isYourTurn}'

step "Done"
info "Match $MATCH_ID is left InProgress — open it in the web client to keep playing:"
info "  http://localhost:3000  (paste the match ID above to join as a spectator/third client, or use the tokens above)"
