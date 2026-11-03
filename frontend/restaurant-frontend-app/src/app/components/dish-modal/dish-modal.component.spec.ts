import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DishListResponse } from '@app/models/dishes/dish-list-response.model';
import { DishModalComponent } from './dish-modal.component';

const DISH: DishListResponse = {
  id: 'dish-1',
  name: 'Caesar Salad',
  imageUrl: 'assets/img/dish.png',
  price: 180,
  state: 'Available',
  weight: '250g',
  dishType: 'Appetizer',
  description: 'Romaine, croutons, parmesan',
  calories: '320',
  proteins: '12',
  carbohydrates: '18',
  fats: '22',
  vitamins: 'A, C',
};

describe('DishModalComponent', () => {
  let component: DishModalComponent;
  let fixture: ComponentFixture<DishModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DishModalComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(DishModalComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('dish', DISH);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
