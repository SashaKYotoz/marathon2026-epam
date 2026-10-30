import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { GroupPreOrderService } from '@app/services/group-pre-order/group-pre-order.service';
import { ToastService } from '@app/services/toast/toast.service';
import { GroupPreOrderPageComponent } from './group-pre-order-page.component';

describe('GroupPreOrderPageComponent', () => {
  let api: jasmine.SpyObj<GroupPreOrderService>;

  async function render(participants: unknown[], total: number) {
    api.getSessions$.and.returnValue(of({ participants, total } as never));

    await TestBed.configureTestingModule({
      imports: [GroupPreOrderPageComponent],
      providers: [
        provideRouter([]),
        { provide: GroupPreOrderService, useValue: api },
        { provide: ToastService, useValue: jasmine.createSpyObj<ToastService>('ToastService', ['success', 'error', 'info']) },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(GroupPreOrderPageComponent);
    fixture.detectChanges();
    await fixture.componentInstance.store.load('res-1');
    fixture.detectChanges();
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

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
    api.getOwnerSession$.and.returnValue(of({ guestSessionId: 'owner-session', groupReservationId: 'group-1' }));
    api.getStatus$.and.returnValue(of({ currentParticipants: 1, maxCapacity: 4, status: 'Open' }));
    api.getOwnerSessionInfo$.and.returnValue(of({ name: 'Owner', items: [], total: 0, isSubmitted: false }));
    api.getOwnerMenu$.and.returnValue(of({ dishes: {} }));
  });

  it('renders every participant with dishes, status and totals', async () => {
    const element = await render(
      [
        {
          id: 'session1',
          type: 'Guest',
          name: 'Andrii',
          items: [{ dish: { id: 'dish1', name: 'Caesar Salad', price: 180 }, quantity: 2 }],
          total: 360,
          isSubmitted: true,
        },
        { id: 'session2', type: 'Guest', name: 'Olena', items: [], total: 0, isSubmitted: false },
      ],
      360,
    );

    const cards = element.querySelectorAll('[data-testid="participant-card"]');
    expect(cards.length).toBe(2);
    expect(cards[0].textContent).toContain('Andrii');
    expect(cards[0].querySelector('[data-testid="participant-status"]')?.textContent).toContain('Confirmed');
    expect(cards[1].querySelector('[data-testid="participant-no-dishes"]')).not.toBeNull();
    expect(element.querySelector('[data-testid="group-total"]')?.getAttribute('data-amount')).toBe('360');
  });

  it('shows the empty state when nobody joined', async () => {
    const element = await render([], 0);

    expect(element.querySelector('[data-testid="participants-empty"]')).not.toBeNull();
    expect(element.querySelector('[data-testid="group-total"]')?.getAttribute('data-amount')).toBe('0');
  });

  it('offers removal only for guests', async () => {
    const element = await render(
      [
        { id: 'owner', type: 'Owner', name: 'Maria', items: [{ dish: { id: 'dish1', name: 'Soup', price: 5 }, quantity: 1 }], total: 5, isSubmitted: false },
        { id: 'session1', type: 'Guest', name: 'Andrii', items: [], total: 0, isSubmitted: false },
      ],
      5,
    );

    expect(element.querySelector('button[aria-label="Remove Maria"]')).toBeNull();
    expect(element.querySelector('button[aria-label="Remove Andrii"]')).not.toBeNull();
  });
});
