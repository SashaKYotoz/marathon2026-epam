import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { PlaceOrderWaiterComponent } from './place-order-waiter.component';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { AuthenticationService } from '@app/auth/services/authentication.service';
import { ToastService } from '@app/services/toast/toast.service';

describe('PlaceOrderWaiterComponent', () => {
  let component: PlaceOrderWaiterComponent;
  let fixture: ComponentFixture<PlaceOrderWaiterComponent>;
  let auth: jasmine.SpyObj<AuthenticationService>;

  beforeEach(async () => {
    auth = jasmine.createSpyObj<AuthenticationService>('AuthenticationService', ['getUser', 'getUserNameById$']);
    auth.getUser.and.returnValue({ id: 'waiter-1', firstName: 'Ivan', lastName: 'Petrenko', email: 'ivan@example.com', role: 'Waiter' });
    auth.getUserNameById$.and.returnValue(of('Olena Shevchenko'));

    await TestBed.configureTestingModule({
      providers: [
        { provide: API_BASE_URL, useValue: 'https://test.api' },
        { provide: AuthenticationService, useValue: auth },
        { provide: ToastService, useValue: jasmine.createSpyObj<ToastService>('ToastService', ['success', 'error', 'info']) },
      ],
      imports: [PlaceOrderWaiterComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(PlaceOrderWaiterComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('customerId', 'customer-1');
    fixture.detectChanges();
  });

  it('should create for a signed-in waiter', () => {
    expect(component).toBeTruthy();
  });

  it('loads the customer name for the order', () => {
    expect(auth.getUserNameById$).toHaveBeenCalledWith('customer-1');
  });
});
