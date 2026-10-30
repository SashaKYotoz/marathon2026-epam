import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { GroupSessionsSnapshot } from '@app/models/group-pre-order/group-pre-order.model';
import { GroupPreOrderService } from '@app/services/group-pre-order/group-pre-order.service';
import { ToastService } from '@app/services/toast/toast.service';
import { GroupPreOrderStore } from './group-pre-order.store';

const SNAPSHOT: GroupSessionsSnapshot = {
  participants: [
    {
      id: 'session1',
      type: 'Guest',
      name: 'Andrii',
      items: [{ dish: { id: 'dish1', name: 'Caesar Salad', price: 180 }, quantity: 2 }],
      total: 360,
      isSubmitted: true,
    },
    {
      id: 'session2',
      type: 'Guest',
      name: 'Olena',
      items: [{ dish: { id: 'dish2', name: 'Tom Yum', price: 220 }, quantity: 1 }],
      total: 220,
      isSubmitted: false,
    },
  ],
  total: 580,
};

describe('GroupPreOrderStore', () => {
  let store: GroupPreOrderStore;
  let api: jasmine.SpyObj<GroupPreOrderService>;
  let toast: jasmine.SpyObj<ToastService>;

  beforeEach(() => {
    api = jasmine.createSpyObj<GroupPreOrderService>('GroupPreOrderService', [
      'getOwnerSession$',
      'getStatus$',
      'getSessions$',
      'removeSession$',
      'submitGroup$',
      'getOwnerSessionInfo$',
      'getOwnerMenu$',
      'addOwnerDish$',
      'removeOwnerDish$',
      'submitOwnerCart$',
    ]);
    toast = jasmine.createSpyObj<ToastService>('ToastService', ['success', 'error', 'info']);

    api.getOwnerSession$.and.returnValue(of({ guestSessionId: 'owner-session', groupReservationId: 'group-1' }));
    api.getStatus$.and.returnValue(of({ currentParticipants: 3, maxCapacity: 4, status: 'Open' }));
    api.getSessions$.and.returnValue(of(SNAPSHOT));
    api.getOwnerSessionInfo$.and.returnValue(of({ name: 'Owner', items: [], total: 0, isSubmitted: false }));
    api.getOwnerMenu$.and.returnValue(of({ dishes: {} }));

    TestBed.configureTestingModule({
      providers: [
        GroupPreOrderStore,
        { provide: GroupPreOrderService, useValue: api },
        { provide: ToastService, useValue: toast },
      ],
    });

    store = TestBed.inject(GroupPreOrderStore);
  });

  it('loads the owner session, participants and totals', async () => {
    await store.load('res-1');

    expect(api.getOwnerSession$).toHaveBeenCalledWith('res-1');
    expect(store.groupReservationId()).toBe('group-1');
    expect(store.participants().length).toBe(2);
    expect(store.groupTotal()).toBe(580);
    expect(store.submittedCount()).toBe(1);
    expect(store.isEditable()).toBeTrue();
  });

  it('shows the backend message when the group cannot be loaded', async () => {
    api.getOwnerSession$.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 403, error: { error: 'Only the reservation owner can perform this action.' } })),
    );

    await store.load('res-1');

    expect(store.loadError()).toBe('Only the reservation owner can perform this action.');
    expect(store.groupReservationId()).toBeNull();
  });

  it('treats an empty group as no participants with a zero total', async () => {
    api.getSessions$.and.returnValue(of({ participants: [], total: 0 }));

    await store.load('res-1');

    expect(store.hasParticipants()).toBeFalse();
    expect(store.groupTotal()).toBe(0);
  });

  it('confirms the group and keeps the consolidated dishes with names', async () => {
    api.submitGroup$.and.returnValue(of([{ dishId: 'dish1', quantity: 2 }]));
    await store.load('res-1');

    await store.submit();

    expect(api.submitGroup$).toHaveBeenCalledWith('group-1', 'res-1');
    expect(store.isSubmitted()).toBeTrue();
    expect(store.consolidatedLines()).toEqual([{ dishId: 'dish1', name: 'Caesar Salad', quantity: 2 }]);
    expect(toast.success).toHaveBeenCalled();
  });

  it('keeps the refusal message when confirming fails', async () => {
    api.submitGroup$.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 400, error: { error: 'Pre-order for this reservation was not found.' } })),
    );
    await store.load('res-1');

    await store.submit();

    expect(store.submitError()).toBe('Pre-order for this reservation was not found.');
    expect(store.isSubmitted()).toBeFalse();
  });

  it('marks the group as submitted when the backend reports a conflict', async () => {
    api.submitGroup$.and.returnValue(throwError(() => new HttpErrorResponse({ status: 409, error: { error: 'Group reservation was submitted.' } })));
    await store.load('res-1');
    api.getStatus$.and.returnValue(of({ currentParticipants: 3, maxCapacity: 4, status: 'Submitted' }));

    await store.submit();

    expect(store.isSubmitted()).toBeTrue();
  });

  it('replaces the snapshot after removing a participant', async () => {
    api.removeSession$.and.returnValue(of({ participants: [SNAPSHOT.participants[1]], total: 220 }));
    await store.load('res-1');
    store.requestRemoval('session1');

    await store.confirmRemoval(SNAPSHOT.participants[0]);

    expect(api.removeSession$).toHaveBeenCalledWith('group-1', 'session1');
    expect(store.participants().map((participant) => participant.id)).toEqual(['session2']);
    expect(store.groupTotal()).toBe(220);
    expect(store.pendingRemovalId()).toBeNull();
  });

  it('does not load the menu once the group was submitted', async () => {
    api.getStatus$.and.returnValue(of({ currentParticipants: 3, maxCapacity: 4, status: 'Submitted' }));

    await store.load('res-1');

    expect(store.isEditable()).toBeFalse();
    expect(api.getOwnerMenu$).not.toHaveBeenCalled();
  });
});
