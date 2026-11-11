import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { StatusBadge } from '../components/StatusBadge';
import type { HoldDto } from '../types';
import { StatCard } from '../components/StatCard';
import { useClasses } from '../hooks/useClasses';
import { useToast } from '../hooks/toastContext';
import { api, getErrorMessage } from '../services/api';

export function AdminPage() {
  const { classes, error, loading, reload } = useClasses();
  const { notify } = useToast();

  const [holds, setHolds] = useState<HoldDto[] | null>(null);
  const [holdsError, setHoldsError] = useState<string | null>(null);
  const loadHolds = useCallback(async () => {
    try {
      setHolds(await api.listAllHolds());
      setHoldsError(null);
    } catch (err) {
      setHoldsError(getErrorMessage(err));
    }
  }, []);
  useEffect(() => {
    void loadHolds();
  }, [loadHolds]);

  const [name, setName] = useState('');
  const [shiftName, setShiftName] = useState('');
  const [seats, setSeats] = useState('');
  const [formError, setFormError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const totals = (classes ?? []).reduce(
    (a, c) => ({
      seats: a.seats + c.totalSeats,
      held: a.held + c.heldSeats,
      confirmed: a.confirmed + c.confirmedSeats,
      available: a.available + c.availableSeats,
    }),
    { seats: 0, held: 0, confirmed: 0, available: 0 },
  );
  const occupancy = totals.seats > 0 ? Math.round(((totals.held + totals.confirmed) / totals.seats) * 100) : 0;

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setFormError(null);

    const totalSeats = Number(seats);
    if (!name.trim() || !shiftName.trim()) return setFormError('Class name and shift are required.');
    if (!Number.isInteger(totalSeats) || totalSeats <= 0) return setFormError('Total seats must be a whole number above zero.');

    setSaving(true);
    try {
      await api.createClass({ name: name.trim(), shiftName: shiftName.trim(), totalSeats });
      notify('success', `Created ${name.trim()} (${shiftName.trim()}) with ${totalSeats} seats.`);
      setName('');
      setShiftName('');
      setSeats('');
      reload();
      void loadHolds();
    } catch (err) {
      setFormError(getErrorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="page">
      <h1>Admin dashboard</h1>

      <section className="stats">
        <StatCard label="Classes" value={classes?.length ?? 0} />
        <StatCard label="Total seats" value={totals.seats} />
        <StatCard label="Held" value={totals.held} tone="warn" />
        <StatCard label="Confirmed" value={totals.confirmed} tone="info" />
        <StatCard label="Available" value={totals.available} tone="ok" />
        <StatCard label="Occupancy" value={`${occupancy}%`} />
      </section>

      <section className="card">
        <h2>Create class and shift</h2>
        <form className="form-row" onSubmit={onSubmit}>
          <label>
            Class name
            <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Class 5" maxLength={100} />
          </label>
          <label>
            Shift
            <input value={shiftName} onChange={(e) => setShiftName(e.target.value)} placeholder="Morning" maxLength={100} />
          </label>
          <label>
            Total seats
            <input type="number" min={1} value={seats} onChange={(e) => setSeats(e.target.value)} placeholder="120" />
          </label>
          <button className="btn btn-primary" type="submit" disabled={saving}>
            {saving ? 'Creating...' : 'Create class'}
          </button>
        </form>
        {formError && <p className="error-text">{formError}</p>}
        <p className="hint">The shift is created automatically if it does not exist yet.</p>
      </section>

      <section className="card">
        <div className="card-head">
          <h2>Classes</h2>
          <button className="btn" onClick={reload}>Refresh</button>
        </div>

        {loading && <p className="muted">Loading classes...</p>}

        {error && (
          <div className="banner banner-error">
            {error} <button className="btn btn-small" onClick={reload}>Retry</button>
          </div>
        )}

        {classes && classes.length === 0 && <p className="muted">No classes yet. Create the first one above.</p>}

        {classes && classes.length > 0 && (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Class</th>
                  <th>Shift</th>
                  <th>Total</th>
                  <th>Held</th>
                  <th>Confirmed</th>
                  <th>Available</th>
                  <th>Filled</th>
                </tr>
              </thead>
              <tbody>
                {classes.map((c) => {
                  const pct = Math.round(((c.heldSeats + c.confirmedSeats) / c.totalSeats) * 100);
                  return (
                    <tr key={c.id}>
                      <td>{c.name}</td>
                      <td>{c.shiftName}</td>
                      <td>{c.totalSeats}</td>
                      <td>{c.heldSeats}</td>
                      <td>{c.confirmedSeats}</td>
                      <td><strong>{c.availableSeats}</strong></td>
                      <td>
                        <div className="bar"><div className="bar-fill" style={{ width: `${pct}%` }} /></div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="card">
        <div className="card-head">
          <h2>Applications</h2>
          <button className="btn" onClick={() => void loadHolds()}>Refresh</button>
        </div>
        {holdsError && <div className="banner banner-error">{holdsError}</div>}
        {holds && holds.length === 0 && <p className="muted">No applications yet.</p>}
        {holds && holds.length > 0 && (
          <ul className="app-list">
            {holds.map((h) => (
              <li key={h.holdId} className="app-item">
                <div className="app-main">
                  <strong>{h.className}</strong>
                  <span className="muted">{h.shiftName} shift</span>
                </div>
                <div className="app-meta">
                  <span>Student: {h.studentId}</span>
                  <span>Applied: {new Date(h.createdAt).toLocaleString()}</span>
                  {h.confirmedAt && <span>Confirmed: {new Date(h.confirmedAt).toLocaleString()}</span>}
                  {h.cancelledAt && <span>Cancelled: {new Date(h.cancelledAt).toLocaleString()}</span>}
                </div>
                <StatusBadge status={h.status} />
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
