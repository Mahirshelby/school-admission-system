import type { HoldStatus } from '../types';

export function StatusBadge({ status }: { status: HoldStatus }) {
  return <span className={`st-badge st-badge-${status.toLowerCase()}`}>{status.toUpperCase()}</span>;
}
