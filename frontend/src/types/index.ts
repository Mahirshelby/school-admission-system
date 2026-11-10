export type HoldStatus = 'Held' | 'Confirmed' | 'Cancelled' | 'Expired';

export interface ClassDto {
  id: number;
  name: string;
  shiftName: string;
  totalSeats: number;
  heldSeats: number;
  confirmedSeats: number;
  availableSeats: number;
}

export interface HoldDto {
  id: string;
  classId: number;
  className: string;
  shiftName: string;
  studentId: string;
  status: HoldStatus;
  createdAt: string;
  expiresAt: string;
  confirmedAt: string | null;
  cancelledAt: string | null;
}

export interface CreateClassRequest {
  name: string;
  shiftName: string;
  totalSeats: number;
}

export interface ApiError {
  code: string;
  message: string;
}
