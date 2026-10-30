import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, inject, untracked } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { GroupParticipant } from '@app/models/group-pre-order/group-pre-order.model';
import { GroupPreOrderStore } from './state/group-pre-order.store';

export const GROUP_PRE_ORDER_POLL_MS = 10000;

@Component({
  selector: 'app-group-pre-order-page',
  imports: [CurrencyPipe, RouterLink],
  providers: [GroupPreOrderStore],
  templateUrl: './group-pre-order-page.component.html',
  styleUrl: './group-pre-order-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GroupPreOrderPageComponent {
  private readonly route = inject(ActivatedRoute);
  readonly store = inject(GroupPreOrderStore);

  readonly reservationId = toSignal(this.route.paramMap.pipe(map((params) => params.get('reservationId') ?? '')), {
    initialValue: '',
  });

  readonly participantViews = computed(() =>
    this.store.participants().map((participant) => ({
      ...participant,
      removable: this.isRemovable(participant),
      statusLabel: participant.isSubmitted ? 'Confirmed' : 'Not confirmed yet',
    })),
  );

  readonly menuRows = computed(() => {
    const quantities = new Map(this.store.ownerItems().map((item) => [item.dish.id, item.quantity]));
    return this.store.menuDishes().map((dish) => ({ dish, quantity: quantities.get(dish.id) ?? 0 }));
  });

  constructor() {
    effect(() => {
      const reservationId = this.reservationId();
      if (reservationId) {
        untracked(() => void this.store.load(reservationId));
      }
    });

    effect((onCleanup) => {
      const groupId = this.store.groupReservationId();
      if (!groupId || !this.store.isEditable()) {
        return;
      }

      const timer = setInterval(() => void this.store.refresh(true), GROUP_PRE_ORDER_POLL_MS);
      onCleanup(() => clearInterval(timer));
    });
  }

  retry(): void {
    const reservationId = this.reservationId();
    if (reservationId) {
      void this.store.load(reservationId);
    }
  }

  private isRemovable(participant: GroupParticipant): boolean {
    return participant.type === 'Guest' && this.store.isEditable();
  }
}
