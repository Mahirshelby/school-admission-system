interface Props {
  label: string;
  value: string | number;
  tone?: 'default' | 'warn' | 'ok' | 'info';
}

export function StatCard({ label, value, tone = 'default' }: Props) {
  return (
    <div className={`stat stat-${tone}`}>
      <div className="stat-value">{value}</div>
      <div className="stat-label">{label}</div>
    </div>
  );
}
