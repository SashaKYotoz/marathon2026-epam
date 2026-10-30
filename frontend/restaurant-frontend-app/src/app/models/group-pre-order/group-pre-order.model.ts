export type GroupParticipantType = 'Owner' | 'Guest';

export interface GroupParticipantDish {
  id: string;
  name: string;
  price: number;
  imageUrl?: string | null;
}

export interface GroupParticipantItem {
  dish: GroupParticipantDish;
  quantity: number;
}

export interface GroupParticipant {
  id: string;
  type: GroupParticipantType;
  name: string;
  items: GroupParticipantItem[];
  total: number;
  isSubmitted: boolean;
}

export interface GroupSessionsSnapshot {
  participants: GroupParticipant[];
  total: number;
}

export interface OwnerGuestSession {
  guestSessionId: string;
  groupReservationId: string;
}

export interface ConsolidatedOrderItem {
  dishId: string;
  quantity: number;
}

export interface OwnerSessionInfo {
  name: string;
  items: GroupParticipantItem[];
  total: number;
  isSubmitted: boolean;
}

export type GroupReservationStatusValue = 'Open' | 'Submitted' | 'Closed';

export interface GroupReservationStatusInfo {
  currentParticipants: number;
  maxCapacity: number;
  status: GroupReservationStatusValue;
}
