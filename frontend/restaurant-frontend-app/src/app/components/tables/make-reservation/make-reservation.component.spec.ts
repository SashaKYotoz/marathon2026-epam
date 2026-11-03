import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Table } from '@app/models/tables/table.model';
import { MakeReservationComponent } from './make-reservation.component';

const TABLE: Table = {
  id: 'table-1',
  tableNumber: 3,
  locationId: 'location-1',
  capacity: 4,
  timeRanges: [{ start: '2030-01-01T18:00:00Z', end: '2030-01-01T19:30:00Z' }],
};

describe('MakeReservationComponent', () => {
  let component: MakeReservationComponent;
  let fixture: ComponentFixture<MakeReservationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MakeReservationComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(MakeReservationComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('locationName', '48 Rustaveli Avenue');
    fixture.componentRef.setInput('table', TABLE);
    fixture.componentRef.setInput('selectedTimeRange', TABLE.timeRanges[0]);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
