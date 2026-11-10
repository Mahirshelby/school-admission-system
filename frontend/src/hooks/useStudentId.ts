import { useCallback, useState } from 'react';

const STORAGE_KEY = 'school-admission.studentId';

export const isValidStudentId = (value: string) => /^\d{17}$/.test(value);

function readStoredId(): string | null {
  try {
    const value = localStorage.getItem(STORAGE_KEY);
    return value && isValidStudentId(value) ? value : null;
  } catch {
    return null;
  }
}

export function useStudentId() {
  const [studentId, setStudentId] = useState<string | null>(readStoredId);

  const save = useCallback((value: string): boolean => {
    const trimmed = value.trim();
    if (!isValidStudentId(trimmed)) return false;
    try {
      localStorage.setItem(STORAGE_KEY, trimmed);
    } catch {
      // Storage can be blocked (private mode); the id still works for this session.
    }
    setStudentId(trimmed);
    return true;
  }, []);

  const clear = useCallback(() => {
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      // Nothing to clean up if storage is unavailable.
    }
    setStudentId(null);
  }, []);

  return { studentId, save, clear };
}
