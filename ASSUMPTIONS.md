# Assumptions

- A "class" is a class offered in one shift (e.g. Class 5 / Morning). It owns the seat count.
- Student identity is the birth registration number sent in the X-Student-Id header. It is validated as 17 digits; this is an assumption, not a rule given in the brief.
- Held and confirmed seats both count as taken. Expired holds are not counted and are marked EXPIRED.
- Confirmed applications can also be cancelled, which frees the seat. The brief does not restrict cancellation.
- Seat allocation uses a PostgreSQL transaction with a row lock on the class (SELECT ... FOR UPDATE).
- Guardians act on behalf of the student: the guardian enters the student's 17-digit registration number in the UI, and that number identifies the applicant.
- Seats are given on a first-come, first-served basis in the order the database lock is acquired; there is no lottery or priority.
- Shifts are not managed separately: a shift is created automatically the first time a class is created with that shift name.
- A hold lasts 10 minutes by default and can be changed with the HOLD_DURATION_SECONDS environment variable.
- A hold with expires_at in the past is treated as Expired as soon as it is read, even before the background sweeper (every 5 seconds) writes the status to the database.
- Confirmed applications never expire. Only Held applications can expire.
- Payment is simulated: the Bengali-labelled "Pay admission fee" button calls the confirm endpoint and no real payment gateway is involved.
- Confirm and cancel are idempotent: repeating them does not change the seat count twice.
- A student can hold seats in several different classes at the same time, but only one active (Held or Confirmed) application per class. After a cancel or expiry the student can apply to that class again.
- The shift is optional when creating a class (POST /classes). If shiftName is omitted, it defaults to "General".
- GET /admin/holds (list of all applications for the Admin page) has no authentication. Authentication and authorization are out of scope for this assignment, so any client can call it.
