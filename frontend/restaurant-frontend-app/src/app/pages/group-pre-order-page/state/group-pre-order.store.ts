import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom, Observable } from 'rxjs';
import {
  ConsolidatedOrderItem,
  GroupParticipant,
  GroupReservationStatusValue,
  GroupSessionsSnapshot,
  OwnerSessionInfo,
} from '@app/models/group-pre-order/group-pre-order.model';
import { GroupPreOrderService } from '@app/services/group-pre-order/group-pre-order.service';
import { GuestMenuDish } from '@app/services/guest-sessions/guest-sessions.service';
import { ToastService } from '@app/services/toast/toast.service';
import { getServerErrorMessage } from '@app/shared/helpers/http-error-helpers';

export interface ConsolidatedLine {
  dishId: string;
  name: string;
  quantity: number;
}

const EMPTY_SNAPSHOT: GroupSessionsSnapshot = { participants: [], total: 0 };

@Injectable()
export class GroupPreOrderStore {
  private readonly api = inject(GroupPreOrderService);
  private readonly toast = inject(ToastService);

  readonly reservationId = signal<string | null>(null);
  readonly ownerSessionId = signal<string | null>(null);
  readonly groupReservationId = signal<string | null>(null);
  readonly groupStatus = signal<GroupReservationStatusValue>('Open');

  readonly snapshot = signal<GroupSessionsSnapshot>(EMPTY_SNAPSHOT);
  readonly loading = signal(false);
  readonly loadError = signal<string | null>(null);

  readonly pendingRemovalId = signal<string | null>(null);
  readonly removingId = signal<string | null>(null);

  readonly submitting = signal(false);
  readonly submitError = signal<string | null>(null);
  readonly submittedItems = signal<ConsolidatedOrderItem[] | null>(null);

  readonly ownerInfo = signal<OwnerSessionInfo | null>(null);
  readonly ownerBusy = signal(false);
  readonly menu = signal<Record<string, GuestMenuDish[]>>({});
  readonly menuLoading = signal(false);

  readonly participants = computed<GroupParticipant[]>(() => this.snapshot().participants ?? []);
  readonly hasParticipants = computed(() => this.participants().length > 0);
  readonly groupTotal = computed(() => this.snapshot().total ?? 0);
  readonly submittedCount = computed(() => this.participants().filter((participant) => participant.isSubmitted).length);
  readonly isSubmitted = computed(() => this.groupStatus() === 'Submitted' || this.submittedItems() !== null);
  readonly isClosed = computed(() => this.groupStatus() === 'Closed');
  readonly isEditable = computed(() => !this.isSubmitted() && !this.isClosed());
  readonly canSubmit = computed(() => this.isEditable() && !!this.groupReservationId() && !this.submitting());

  readonly ownerItems = computed(() => this.ownerInfo()?.items ?? []);
  readonly ownerTotal = computed(() => this.ownerInfo()?.total ?? 0);
  readonly ownerSubmitted = computed(() => this.ownerInfo()?.isSubmitted ?? false);
  readonly canEditOwnerCart = computed(() => this.isEditable() && !this.ownerSubmitted() && !this.ownerBusy());

  readonly menuDishes = computed(() =>
    Object.values(this.menu())
      .flat()
      .filter((dish) => dish.selectable),
  );

  readonly consolidatedLines = computed<ConsolidatedLine[]>(() => {
    const items = this.submittedItems() ?? [];
    const names = new Map<string, string>();
    this.participants().forEach((participant) =>
      participant.items.forEach((item) => names.set(item.dish.id, item.dish.name)),
    );
    this.menuDishes().forEach((dish) => names.set(dish.id, dish.name));
    return items.map((item) => ({ dishId: item.dishId, quantity: item.quantity, name: names.get(item.dishId) ?? item.dishId }));
  });

  async load(reservationId: string): Promise<void> {
    if (!reservationId) {
      return;
    }

    this.reservationId.set(reservationId);
    this.loading.set(true);
    this.loadError.set(null);

    try {
      const owner = await firstValueFrom(this.api.getOwnerSession$(reservationId));
      this.ownerSessionId.set(owner.guestSessionId);
      this.groupReservationId.set(owner.groupReservationId);

      await this.loadStatus();
      await Promise.all([this.refresh(), this.loadOwnerInfo(), this.loadMenu()]);
    } catch (error) {
      this.loadError.set(this.messageOf(error, 'The group pre-order could not be loaded.'));
      if (error instanceof HttpErrorResponse && error.status === 409) {
        this.groupStatus.set('Submitted');
      }
    } finally {
      this.loading.set(false);
    }
  }

  async refresh(silent = false): Promise<void> {
    const groupId = this.groupReservationId();
    if (!groupId) {
      return;
    }

    try {
      this.snapshot.set(await firstValueFrom(this.api.getSessions$(groupId)));
    } catch (error) {
      if (!silent) {
        this.loadError.set(this.messageOf(error, 'Participants could not be loaded.'));
      }
    }
  }

  requestRemoval(participantId: string): void {
    this.pendingRemovalId.set(participantId);
  }

  cancelRemoval(): void {
    this.pendingRemovalId.set(null);
  }

  async confirmRemoval(participant: GroupParticipant): Promise<void> {
    const groupId = this.groupReservationId();
    if (!groupId || this.removingId()) {
      return;
    }

    this.removingId.set(participant.id);
    try {
      this.snapshot.set(await firstValueFrom(this.api.removeSession$(groupId, participant.id)));
      this.toast.success(`${participant.name} was removed from the group.`, 'Participant removed');
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 409) {
        await this.loadStatus();
      }
    } finally {
      this.removingId.set(null);
      this.pendingRemovalId.set(null);
    }
  }

  async submit(): Promise<void> {
    const groupId = this.groupReservationId();
    const reservationId = this.reservationId();
    if (!groupId || !reservationId || this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.submitError.set(null);
    try {
      const items = await firstValueFrom(this.api.submitGroup$(groupId, reservationId));
      this.submittedItems.set(items);
      this.groupStatus.set('Submitted');
      this.toast.success('The group pre-order has been sent to the restaurant.', 'Pre-order confirmed');
      await this.refresh(true);
    } catch (error) {
      this.submitError.set(this.messageOf(error, 'The group pre-order could not be confirmed.'));
      if (error instanceof HttpErrorResponse && error.status === 409) {
        await this.loadStatus();
      }
    } finally {
      this.submitting.set(false);
    }
  }

  async addOwnerDish(dishId: string): Promise<void> {
    await this.changeOwnerCart((sessionId) => this.api.addOwnerDish$(sessionId, dishId, 1));
  }

  async removeOwnerDish(dishId: string): Promise<void> {
    await this.changeOwnerCart((sessionId) => this.api.removeOwnerDish$(sessionId, dishId, 1));
  }

  async submitOwnerCart(): Promise<void> {
    await this.changeOwnerCart((sessionId) => this.api.submitOwnerCart$(sessionId));
  }

  private async changeOwnerCart(action: (sessionId: string) => Observable<unknown>): Promise<void> {
    const sessionId = this.ownerSessionId();
    if (!sessionId || this.ownerBusy()) {
      return;
    }

    this.ownerBusy.set(true);
    try {
      await firstValueFrom(action(sessionId));
      await Promise.all([this.loadOwnerInfo(), this.refresh(true)]);
    } catch {
      await this.loadOwnerInfo();
    } finally {
      this.ownerBusy.set(false);
    }
  }

  private async loadStatus(): Promise<void> {
    const groupId = this.groupReservationId();
    if (!groupId) {
      return;
    }

    try {
      const status = await firstValueFrom(this.api.getStatus$(groupId));
      this.groupStatus.set(status.status);
    } catch {
      this.groupStatus.set('Open');
    }
  }

  private async loadOwnerInfo(): Promise<void> {
    const sessionId = this.ownerSessionId();
    if (!sessionId) {
      return;
    }

    try {
      this.ownerInfo.set(await firstValueFrom(this.api.getOwnerSessionInfo$(sessionId)));
    } catch {
      this.ownerInfo.set(null);
    }
  }

  private async loadMenu(): Promise<void> {
    const sessionId = this.ownerSessionId();
    if (!sessionId || !this.isEditable()) {
      return;
    }

    this.menuLoading.set(true);
    try {
      const response = await firstValueFrom(this.api.getOwnerMenu$(sessionId));
      this.menu.set(response.dishes ?? {});
    } catch {
      this.menu.set({});
    } finally {
      this.menuLoading.set(false);
    }
  }

  private messageOf(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      return getServerErrorMessage(error) || fallback;
    }
    return fallback;
  }
}
