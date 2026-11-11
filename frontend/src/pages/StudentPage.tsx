import { useState, type FormEvent } from 'react';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { StatusBadge } from '../components/StatusBadge';
import { useToast } from '../hooks/toastContext';
import { useClasses } from '../hooks/useClasses';
import { formatRemaining, useCountdown } from '../hooks/useCountdown';
import { useHolds } from '../hooks/useHolds';
import { useStudentId } from '../hooks/useStudentId';
import { api, getErrorMessage } from '../services/api';
import type { ClassDto, HoldDto, HoldStatus } from '../types';

const PAYMENT_SIMULATION_MS = 800;

const formatDate = (value: string) => new Date(value).toLocaleString();

function RegistrationForm({ onSubmit }: { onSubmit: (value: string) => boolean }) {
  const [value, setValue] = useState('');
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    if (!onSubmit(value)) {
      setError('The birth registration number must be exactly 17 digits.');
    }
  };

  return (
    <form className="st-card st-register" onSubmit={handleSubmit}>
      <h1>Student admission</h1>
      <p className="st-muted">
        Enter your birth registration number to apply for a class and see your applications.
      </p>
      <label htmlFor="st-reg">Birth registration number</label>
      <input
        id="st-reg"
        inputMode="numeric"
        autoComplete="off"
        maxLength={17}
        placeholder="17 digits"
        value={value}
        onChange={(event) => {
          setValue(event.target.value.replace(/\D/g, ''));
          setError(null);
        }}
      />
      {error && (
        <p className="st-error" role="alert">
          {error}
        </p>
      )}
      <button type="submit" className="st-btn st-btn-primary">
        Continue
      </button>
    </form>
  );
}

interface HoldRowProps {
  hold: HoldDto;
  busy: boolean;
  onPay: (hold: HoldDto) => void;
  onCancel: (hold: HoldDto) => void;
}

function HoldRow({ hold, busy, onPay, onCancel }: HoldRowProps) {
  const remaining = useCountdown(hold.status === 'HELD' ? hold.expiresAt : null);
  // The server flips Held to Expired on its next read; the page does not wait for that.
  const status: HoldStatus = hold.status === 'HELD' && remaining === 0 ? 'EXPIRED' : hold.status;
  const isHeld = status === 'HELD';
  const isExpired = status === 'EXPIRED';
  const isConfirmed = status === 'CONFIRMED';

  return (
    <li className={`st-hold st-hold-${status.toLowerCase()}`}>
      <div className="st-hold-main">
        <strong>{hold.className}</strong>
        <span className="st-muted">{hold.shiftName} shift</span>
        <span className="st-muted">Applied {formatDate(hold.createdAt)}</span>
      </div>

      <div className="st-hold-status">
        <StatusBadge status={status} />
        {isHeld && <span className="st-timer">Expires in {formatRemaining(remaining)}</span>}
        {isExpired && (
          <span className="st-expired-note">This hold expired and the seat was released.</span>
        )}
        {isConfirmed && hold.confirmedAt && (
          <span className="st-muted">Confirmed {formatDate(hold.confirmedAt)}</span>
        )}
        {status === 'CANCELLED' && hold.cancelledAt && (
          <span className="st-muted">Cancelled {formatDate(hold.cancelledAt)}</span>
        )}
      </div>

      <div className="st-hold-actions">
        {(isHeld || isExpired) && (
          <button
            type="button"
            className="st-btn st-btn-primary"
            disabled={busy || isExpired}
            onClick={() => onPay(hold)}
          >
            {busy ? 'Processing payment...' : 'ভর্তি ফি দিন'}
          </button>
        )}
        {(isHeld || isConfirmed) && (
          <button
            type="button"
            className="st-btn st-btn-ghost"
            disabled={busy}
            onClick={() => onCancel(hold)}
          >
            Cancel
          </button>
        )}
      </div>
    </li>
  );
}

interface DashboardProps {
  studentId: string;
  onSwitch: () => void;
}

function StudentDashboard({ studentId, onSwitch }: DashboardProps) {
  const { notify } = useToast();
  const { classes, error: classesError, loading: classesLoading, reload: reloadClasses } = useClasses();
  const { holds, error: holdsError, loading: holdsLoading, refresh: refreshHolds } = useHolds(studentId);
  const [applyingId, setApplyingId] = useState<number | null>(null);
  const [busyHoldId, setBusyHoldId] = useState<string | null>(null);
  const [pendingCancel, setPendingCancel] = useState<HoldDto | null>(null);

  const refreshAll = async () => {
    await refreshHolds();
    void reloadClasses();
  };

  const hasActiveHold = (classId: number) =>
    holds.some((h) => h.classId === classId && (h.status === 'HELD' || h.status === 'CONFIRMED'));

  const handleApply = async (cls: ClassDto) => {
    setApplyingId(cls.id);
    try {
      await api.createHold(cls.id, studentId);
      notify('success', `Seat held in ${cls.name} (${cls.shiftName}). Complete payment before the timer ends.`);
    } catch (err) {
      notify('error', getErrorMessage(err));
    } finally {
      setApplyingId(null);
      await refreshAll();
    }
  };

  const handlePay = async (hold: HoldDto) => {
    setBusyHoldId(hold.holdId);
    try {
      // Simulated payment: no real gateway, just a short wait before confirming.
      await new Promise((resolve) => setTimeout(resolve, PAYMENT_SIMULATION_MS));
      await api.confirmHold(hold.holdId, studentId);
      notify('success', 'Payment received. Your admission is confirmed.');
    } catch (err) {
      notify('error', getErrorMessage(err));
    } finally {
      setBusyHoldId(null);
      await refreshAll();
    }
  };

  const handleCancel = async () => {
    if (!pendingCancel) return;
    const hold = pendingCancel;
    setBusyHoldId(hold.holdId);
    try {
      await api.cancelHold(hold.holdId, studentId);
      notify('success', 'Application cancelled and the seat was released.');
    } catch (err) {
      notify('error', getErrorMessage(err));
    } finally {
      setBusyHoldId(null);
      setPendingCancel(null);
      await refreshAll();
    }
  };

  return (
    <div className="st-page">
      <div className="st-page-head">
        <div>
          <h1>Student admission</h1>
          <p className="st-muted">
            Registration number: <strong>{studentId}</strong>
          </p>
        </div>
        <button type="button" className="st-btn st-btn-ghost" onClick={onSwitch}>
          Switch student
        </button>
      </div>

      <section className="st-section">
        <h2>Available classes</h2>
        {classesError && (
          <div className="st-alert" role="alert">
            <span>{classesError}</span>
            <button type="button" className="st-btn st-btn-ghost" onClick={() => void reloadClasses()}>
              Retry
            </button>
          </div>
        )}
        {classesLoading && <p className="st-muted">Loading classes...</p>}
        {classes && classes.length === 0 && !classesError && (
          <p className="st-empty">No classes are open for admission yet.</p>
        )}
        {classes && classes.length > 0 && (
          <div className="st-class-grid">
            {classes.map((cls) => {
              const applied = hasActiveHold(cls.id);
              const full = cls.availableSeats <= 0;
              const applying = applyingId === cls.id;
              return (
                <article key={cls.id} className="st-card st-class-card">
                  <h3>{cls.name}</h3>
                  <p className="st-muted">{cls.shiftName} shift</p>
                  <p className="st-seats">
                    <strong>{cls.availableSeats}</strong> of {cls.totalSeats} seats available<span className="st-meter"><span style={{ width: `${((cls.totalSeats - cls.availableSeats) / cls.totalSeats) * 100}%` }} /></span>
                  </p>
                  <button
                    type="button"
                    className="st-btn st-btn-primary"
                    disabled={applyingId !== null || applied || full}
                    onClick={() => void handleApply(cls)}
                  >
                    {applying ? 'Applying...' : applied ? 'Applied' : full ? 'Full' : 'Apply'}
                  </button>
                </article>
              );
            })}
          </div>
        )}
      </section>

      <section className="st-section">
        <h2>My applications</h2>
        {holdsError && (
          <div className="st-alert" role="alert">
            <span>{holdsError}</span>
            <button type="button" className="st-btn st-btn-ghost" onClick={() => void refreshHolds()}>
              Retry
            </button>
          </div>
        )}
        {holdsLoading && <p className="st-muted">Loading your applications...</p>}
        {!holdsLoading && !holdsError && holds.length === 0 && (
          <p className="st-empty">You have not applied to any class yet.</p>
        )}
        {holds.length > 0 && (
          <ul className="st-hold-list">
            {holds.map((hold) => (
              <HoldRow
                key={hold.holdId}
                hold={hold}
                busy={busyHoldId === hold.holdId}
                onPay={(h) => void handlePay(h)}
                onCancel={setPendingCancel}
              />
            ))}
          </ul>
        )}
      </section>

      <ConfirmDialog
        open={pendingCancel !== null}
        title="Cancel this application?"
        message={
          pendingCancel?.status === 'CONFIRMED'
            ? 'Your admission is already confirmed. Cancelling will give up the seat and cannot be undone.'
            : 'Your held seat will be released and other students can take it.'
        }
        confirmLabel="Yes, cancel"
        busy={busyHoldId !== null && busyHoldId === pendingCancel?.holdId}
        onConfirm={() => void handleCancel()}
        onCancel={() => setPendingCancel(null)}
      />
    </div>
  );
}

export function StudentPage() {
  const { studentId, save, clear } = useStudentId();
  if (!studentId) return <RegistrationForm onSubmit={save} />;
  return <StudentDashboard studentId={studentId} onSwitch={clear} />;
}

