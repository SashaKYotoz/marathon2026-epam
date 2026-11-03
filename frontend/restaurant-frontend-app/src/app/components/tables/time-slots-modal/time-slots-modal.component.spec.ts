import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Table } from '@app/models/tables/table.model';
import { TimeSlotsModalComponent } from './time-slots-modal.component';

const TABLE: Table = {
  id: 'table-1',
  tableNumber: 3,
  locationId: 'location-1',
  capacity: 4,
  timeRanges: [{ start: '2030-01-01T18:00:00Z', end: '2030-01-01T19:30:00Z' }],
};

describe('TimeSlotsModalComponent', () => {
  let component: TimeSlotsModalComponent;
  let fixture: ComponentFixture<TimeSlotsModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TimeSlotsModalComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(TimeSlotsModalComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('locationName', '48 Rustaveli Avenue');
    fixture.componentRef.setInput('date', '2030-01-01');
    fixture.componentRef.setInput('table', TABLE);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
