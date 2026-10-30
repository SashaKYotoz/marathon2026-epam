import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthenticationService } from '@app/auth/services/authentication.service';
import {
  ConsolidatedOrderItem,
  GroupReservationStatusInfo,
  GroupSessionsSnapshot,
  OwnerGuestSession,
  OwnerSessionInfo,
} from '@app/models/group-pre-order/group-pre-order.model';
import { GuestMenuResponse } from '@app/services/guest-sessions/guest-sessions.service';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

@Injectable({ providedIn: 'root' })
export class GroupPreOrderService {
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthenticationService);

  getOwnerSession$(reservationId: string): Observable<OwnerGuestSession> {
    return this.http.get<OwnerGuestSession>(
      `${this.baseUrl}/reservations/${encodeURIComponent(reservationId)}/guest-session-id`,
    );
  }

  getStatus$(groupReservationId: string): Observable<GroupReservationStatusInfo> {
    return this.http.get<GroupReservationStatusInfo>(
      `${this.baseUrl}/guest/groups/${encodeURIComponent(groupReservationId)}/status`,
    );
  }

  getSessions$(groupReservationId: string): Observable<GroupSessionsSnapshot> {
    return this.http.get<GroupSessionsSnapshot>(
      `${this.baseUrl}/guest/groups/${encodeURIComponent(groupReservationId)}/sessions`,
      { headers: this.ownerHeaders() },
    );
  }

  removeSession$(groupReservationId: string, guestSessionId: string): Observable<GroupSessionsSnapshot> {
    return this.http.delete<GroupSessionsSnapshot>(
      `${this.baseUrl}/guest/groups/${encodeURIComponent(groupReservationId)}/sessions/${encodeURIComponent(guestSessionId)}`,
      { headers: this.ownerHeaders() },
    );
  }

  submitGroup$(groupReservationId: string, reservationId: string): Observable<ConsolidatedOrderItem[]> {
    return this.http.post<ConsolidatedOrderItem[]>(
      `${this.baseUrl}/group-reservations/${encodeURIComponent(groupReservationId)}/submit`,
      { reservationId },
    );
  }

  getOwnerSessionInfo$(guestSessionId: string): Observable<OwnerSessionInfo> {
    return this.http.get<OwnerSessionInfo>(
      `${this.baseUrl}/guest/sessions/${encodeURIComponent(guestSessionId)}`,
      { headers: this.ownerHeaders() },
    );
  }

  getOwnerMenu$(guestSessionId: string): Observable<GuestMenuResponse> {
    return this.http.get<GuestMenuResponse>(
      `${this.baseUrl}/guest/sessions/${encodeURIComponent(guestSessionId)}/menu`,
      { headers: this.ownerHeaders() },
    );
  }

  addOwnerDish$(guestSessionId: string, dishId: string, quantity: number): Observable<unknown> {
    return this.http.post(
      `${this.baseUrl}/guest/sessions/${encodeURIComponent(guestSessionId)}/dishes`,
      { dishId, quantity },
      { headers: this.ownerHeaders() },
    );
  }

  removeOwnerDish$(guestSessionId: string, dishId: string, quantity: number): Observable<unknown> {
    return this.http.request('delete', `${this.baseUrl}/guest/sessions/${encodeURIComponent(guestSessionId)}/dishes`, {
      body: { dishId, quantity },
      headers: this.ownerHeaders(),
    });
  }

  submitOwnerCart$(guestSessionId: string): Observable<unknown> {
    return this.http.post(
      `${this.baseUrl}/guest/sessions/${encodeURIComponent(guestSessionId)}/submit`,
      {},
      { headers: this.ownerHeaders() },
    );
  }

  private ownerHeaders(): HttpHeaders {
    const idToken = this.authService.getTokens()?.idToken;
    return idToken ? new HttpHeaders({ Authorization: `Bearer ${idToken}` }) : new HttpHeaders();
  }
}
