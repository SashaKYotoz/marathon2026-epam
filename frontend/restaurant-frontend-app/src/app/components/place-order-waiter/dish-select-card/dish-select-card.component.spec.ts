import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DishListResponse } from '@app/models/dishes/dish-list-response.model';
import { DishSelectCardComponent } from './dish-select-card.component';

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

describe('DishSelectCardComponent', () => {
  let component: DishSelectCardComponent;
  let fixture: ComponentFixture<DishSelectCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DishSelectCardComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(DishSelectCardComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('dish', DISH);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
