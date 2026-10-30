import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthenticationService } from '@app/auth/services/authentication.service';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { GroupPreOrderService } from './group-pre-order.service';

describe('GroupPreOrderService', () => {
  const baseUrl = 'https://test.api/develop';
  let service: GroupPreOrderService;
  let httpMock: HttpTestingController;
  let authService: jasmine.SpyObj<AuthenticationService>;

  beforeEach(() => {
    authService = jasmine.createSpyObj<AuthenticationService>('AuthenticationService', ['getTokens']);
    authService.getTokens.and.returnValue({ idToken: 'owner-id-token', accessToken: 'access' });

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: baseUrl },
        { provide: AuthenticationService, useValue: authService },
      ],
    });

    service = TestBed.inject(GroupPreOrderService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('requests the owner session of a reservation', () => {
    let result: unknown;
    service.getOwnerSession$('res 1').subscribe((value) => (result = value));

    const req = httpMock.expectOne(`${baseUrl}/reservations/res%201/guest-session-id`);
    expect(req.request.method).toBe('GET');
    req.flush({ guestSessionId: 'owner-session', groupReservationId: 'group-1' });

    expect(result).toEqual({ guestSessionId: 'owner-session', groupReservationId: 'group-1' });
  });

  it('sends the owner id token when reading group sessions', () => {
    service.getSessions$('group-1').subscribe();

    const req = httpMock.expectOne(`${baseUrl}/guest/groups/group-1/sessions`);
    expect(req.request.method).toBe('GET');
    expect(req.request.headers.get('Authorization')).toBe('Bearer owner-id-token');
    req.flush({ participants: [], total: 0 });
  });

  it('omits the Authorization header when the user is signed out', () => {
    authService.getTokens.and.returnValue(null);
    service.getSessions$('group-1').subscribe();

    const req = httpMock.expectOne(`${baseUrl}/guest/groups/group-1/sessions`);
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({ participants: [], total: 0 });
  });

  it('deletes a guest session from the group', () => {
    service.removeSession$('group-1', 'session-2').subscribe();

    const req = httpMock.expectOne(`${baseUrl}/guest/groups/group-1/sessions/session-2`);
    expect(req.request.method).toBe('DELETE');
    expect(req.request.headers.get('Authorization')).toBe('Bearer owner-id-token');
    req.flush({ participants: [], total: 0 });
  });

  it('submits the group pre-order with the reservation id', () => {
    let result: unknown;
    service.submitGroup$('group-1', 'res-1').subscribe((value) => (result = value));

    const req = httpMock.expectOne(`${baseUrl}/group-reservations/group-1/submit`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ reservationId: 'res-1' });
    req.flush([{ dishId: 'dish1', quantity: 3 }]);

    expect(result).toEqual([{ dishId: 'dish1', quantity: 3 }]);
  });

  it('propagates a refused submit as an error', () => {
    let status = 0;
    service.submitGroup$('group-1', 'res-1').subscribe({ error: (error) => (status = error.status) });

    httpMock
      .expectOne(`${baseUrl}/group-reservations/group-1/submit`)
      .flush({ error: 'There are no submitted guest sessions to confirm.' }, { status: 400, statusText: 'Bad Request' });

    expect(status).toBe(400);
  });

  it('adds a dish to the owner session with the id token', () => {
    service.addOwnerDish$('owner-session', 'dish1', 1).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/guest/sessions/owner-session/dishes`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ dishId: 'dish1', quantity: 1 });
    expect(req.request.headers.get('Authorization')).toBe('Bearer owner-id-token');
    req.flush({ message: 'ok' });
  });

  it('removes a dish from the owner session', () => {
    service.removeOwnerDish$('owner-session', 'dish1', 1).subscribe();

    const req = httpMock.expectOne(`${baseUrl}/guest/sessions/owner-session/dishes`);
    expect(req.request.method).toBe('DELETE');
    expect(req.request.body).toEqual({ dishId: 'dish1', quantity: 1 });
    req.flush({ message: 'ok' });
  });

  it('reads the group status', () => {
    service.getStatus$('group-1').subscribe();

    const req = httpMock.expectOne(`${baseUrl}/guest/groups/group-1/status`);
    expect(req.request.method).toBe('GET');
    req.flush({ currentParticipants: 1, maxCapacity: 4, status: 'Open' });
  });
});
