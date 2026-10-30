# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-10-30
### Added
    -  GET /guest/groups/{groupReservationId}/sessions: owner snapshot of group pre-order participants, items and totals
    -  GET /reservations/{reservationId}/guest-session-id: owner's own guest session (created on first call)
    -  POST /group-reservations/{groupReservationId}/submit: consolidated group pre-order submission
    -  Owner can use the own guest session with the Cognito id token
    -  GroupPreOrderService unit tests

### Changed
    -  DELETE /guest/groups/{groupReservationId}/sessions/{guestSessionId} returns the camelCase snapshot, rejects owner session and submitted groups
    -  Owner session is not counted as a joined guest for the guest limit
    -  API Gateway resource /guest/group/{id}/sessions renamed to /guest/groups/{id}/sessions

## [1.0.0] - yyyy-MM-dd
### Added
    -  Added items

### Changed
    -  Changed items 

### Removed
    -  Removed items 
