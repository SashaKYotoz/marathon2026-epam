import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { CardReservationComponent } from './card-reservation.component';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { ReservationCustomer, ReservationStatusCustomer } from '@app/models/reservations/reservation.model';
import { Roles } from '@app/shared/constants/roles';

function reservation(overrides: Partial<ReservationCustomer> = {}): ReservationCustomer {
  return {
    id: 'res-1',
    tableId: 'table-1',
    startTime: new Date('2030-01-01T18:00:00Z'),
    endTime: new Date('2030-01-01T19:30:00Z'),
    guestCount: 4,
    status: ReservationStatusCustomer.Reserved,
    restaurantName: 'Green & Tasty',
    locationAddress: '48 Rustaveli Avenue',
    waiterId: 'waiter-1',
    ...overrides,
  };
}

describe('CardReservationComponent', () => {
  let component: CardReservationComponent;
  let fixture: ComponentFixture<CardReservationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: API_BASE_URL, useValue: 'https://test.api' }],
      imports: [CardReservationComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(CardReservationComponent);
    component = fixture.componentInstance;
  });

  function groupButton(): HTMLButtonElement | null {
    return (fixture.nativeElement as HTMLElement).querySelector('button.btn-group-preorder');
  }

  it('offers a group pre-order for a reserved booking with several guests', () => {
    fixture.componentRef.setInput('r', reservation());
    fixture.detectChanges();

    expect(groupButton()?.textContent).toContain('Group pre-order');
  });

  it('does not offer a group pre-order for a single guest', () => {
    fixture.componentRef.setInput('r', reservation({ guestCount: 1 }));
    fixture.detectChanges();

    expect(groupButton()).toBeNull();
  });

  it('does not offer a group pre-order once the booking is cancelled', () => {
    fixture.componentRef.setInput('r', reservation({ status: ReservationStatusCustomer.Cancelled }));
    fixture.detectChanges();

    expect(groupButton()).toBeNull();
  });

  it('does not offer a group pre-order to a waiter', () => {
    fixture.componentRef.setInput('r', reservation());
    fixture.componentRef.setInput('role', Roles.WAITER);
    fixture.detectChanges();

    expect(groupButton()).toBeNull();
  });

  it('navigates to the group pre-order page', () => {
    const router = TestBed.inject(Router);
    const navigate = spyOn(router, 'navigate').and.resolveTo(true);
    fixture.componentRef.setInput('r', reservation());
    fixture.detectChanges();

    groupButton()?.click();

    expect(navigate).toHaveBeenCalledWith(['/group-pre-order', 'res-1']);
    expect(component.canOpenGroupPreOrder()).toBeTrue();
  });
});
