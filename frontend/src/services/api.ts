import axios from 'axios';
import type { ApiError, ClassDto, CreateClassRequest, HoldDto } from '../types';

const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:8080';

const http = axios.create({ baseURL: API_URL, timeout: 15000 });

const asStudent = (studentId: string) => ({ headers: { 'X-Student-Id': studentId } });

export const api = {
  // Admin
  listClasses: () => http.get<ClassDto[]>('/classes').then((r) => r.data),
  createClass: (body: CreateClassRequest) => http.post<ClassDto>('/classes', body).then((r) => r.data),

  // Student
  createHold: (classId: number, studentId: string) =>
    http.post<HoldDto>(`/classes/${classId}/holds`, null, asStudent(studentId)).then((r) => r.data),
  getHold: (holdId: string, studentId: string) =>
    http.get<HoldDto>(`/holds/${holdId}`, asStudent(studentId)).then((r) => r.data),
  confirmHold: (holdId: string, studentId: string) =>
    http.post<HoldDto>(`/holds/${holdId}/confirm`, null, asStudent(studentId)).then((r) => r.data),
  cancelHold: (holdId: string, studentId: string) =>
    http.delete(`/holds/${holdId}`, asStudent(studentId)).then(() => undefined),
  listAllHolds: () => http.get<HoldDto[]>('/admin/holds').then((r) => r.data),
  myHolds: (studentId: string) => http.get<HoldDto[]>('/me/holds', asStudent(studentId)).then((r) => r.data),
};

// Turns any failure into a message safe to show the user.
export function getErrorMessage(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data as Partial<ApiError> | undefined;
    if (data?.message) return data.message;
    if (!err.response) return 'Cannot reach the server. Please check that the backend is running.';
  }
  return 'Something went wrong. Please try again.';
}
