import { createContext, useContext } from 'react';

export type ToastKind = 'success' | 'error';

export interface ToastApi {
  notify: (kind: ToastKind, text: string) => void;
}

export const ToastContext = createContext<ToastApi>({ notify: () => {} });

export const useToast = () => useContext(ToastContext);
