import { useCallback, useEffect, useState } from 'react';
import { api, getErrorMessage } from '../services/api';
import type { ClassDto } from '../types';

// Loads the class list and refreshes it every few seconds so seat counts stay current.
export function useClasses(pollMs = 5000) {
  const [classes, setClasses] = useState<ClassDto[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [tick, setTick] = useState(0);

  useEffect(() => {
    let active = true;
    const load = async () => {
      try {
        const data = await api.listClasses();
        if (active) {
          setClasses(data);
          setError(null);
        }
      } catch (e) {
        if (active) setError(getErrorMessage(e));
      }
    };
    load();
    const id = setInterval(load, pollMs);
    return () => {
      active = false;
      clearInterval(id);
    };
  }, [pollMs, tick]);

  const reload = useCallback(() => setTick((t) => t + 1), []);
  return { classes, error, loading: classes === null && error === null, reload };
}
