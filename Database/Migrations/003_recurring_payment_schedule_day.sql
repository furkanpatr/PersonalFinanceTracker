ALTER TABLE recurring_payments
ADD COLUMN schedule_day SMALLINT;

UPDATE recurring_payments
SET schedule_day =
    EXTRACT(DAY FROM next_due_date)::SMALLINT;

ALTER TABLE recurring_payments
ALTER COLUMN schedule_day SET NOT NULL;

ALTER TABLE recurring_payments
ADD CONSTRAINT ck_recurring_payments_schedule_day
CHECK (schedule_day BETWEEN 1 AND 31);
