import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ReservationConfirmedComponent } from './reservation-confirmed.component';
import { Reservation, ReservationStatus } from '@app/models/reservations/reservation.model';

describe('ReservationConfirmedComponent', () => {
  let component: ReservationConfirmedComponent;
  let fixture: ComponentFixture<ReservationConfirmedComponent>;

  const reservation: Reservation = {
    id: 'res-1',
    tableId: 'table-1',
    customerId: 'customer-1',
    startTime: new Date('2030-01-01T18:00:00Z'),
    endTime: new Date('2030-01-01T19:30:00Z'),
    guestCount: 4,
    status: ReservationStatus.Reserved,
    locationAddress: '48 Rustaveli Avenue',
    tableCapacity: 4,
    waiterId: 'waiter-1',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReservationConfirmedComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ReservationConfirmedComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('reservation', reservation);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('emits closed once when closed twice', () => {
    const closed = jasmine.createSpy('closed');
    component.closed.subscribe(closed);

    component.onClose();
    component.onClose();

    expect(closed).toHaveBeenCalledTimes(1);
  });
});
