import { useCallback, useEffect, useState } from 'react';
import { api, getErrorMessage } from '../services/api';
import type { HoldDto } from '../types';

const POLL_INTERVAL_MS = 5000;

interface HoldsState {
  studentId: string;
  items: HoldDto[];
  error: string | null;
}

export function useHolds(studentId: string | null) {
  const [state, setState] = useState<HoldsState | null>(null);

  const refresh = useCallback(async () => {
    if (!studentId) return;
    try {
      const data = await api.myHolds(studentId);
      const items = [...data].sort((a, b) => b.createdAt.localeCompare(a.createdAt));
      setState({ studentId, items, error: null });
    } catch (err) {
      setState((prev) => ({
        studentId,
        items: prev?.studentId === studentId ? prev.items : [],
        error: getErrorMessage(err),
      }));
    }
  }, [studentId]);

  useEffect(() => {
    if (!studentId) return;
    void refresh();
    const timer = setInterval(() => void refresh(), POLL_INTERVAL_MS);
    return () => clearInterval(timer);
  }, [studentId, refresh]);

  // Data that belongs to a previously used student id is never shown.
  const current = studentId && state?.studentId === studentId ? state : null;

  return {
    holds: current?.items ?? [],
    error: current?.error ?? null,
    loading: studentId !== null && current === null,
    refresh,
  };
}
