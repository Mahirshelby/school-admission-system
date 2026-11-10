# Assumptions

- A "class" is a class offered in one shift (e.g. Class 5 / Morning). It owns the seat count.
- Student identity is the birth registration number sent in the X-Student-Id header. It is validated as 17 digits; this is an assumption, not a rule given in the brief.
- Held and confirmed seats both count as taken. Expired holds are not counted and are marked EXPIRED.
- Confirmed applications can also be cancelled, which frees the seat. The brief does not restrict cancellation.
- Seat allocation uses a PostgreSQL transaction with a row lock on the class (SELECT ... FOR UPDATE).
