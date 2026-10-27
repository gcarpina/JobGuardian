ALTER TABLE jobguardian_execution_history
    ADD COLUMN lease_acquired_at_utc TIMESTAMPTZ NULL;

ALTER TABLE jobguardian_execution_history
    ADD COLUMN outcome_reason VARCHAR(100) NULL;

CREATE INDEX ix_jobguardian_execution_history_job_started
    ON jobguardian_execution_history
    (
        tenant_id,
        job_namespace,
        job_name,
        started_at_utc DESC,
        execution_id DESC
    );

CREATE INDEX ix_jobguardian_execution_history_incomplete_acquired
    ON jobguardian_execution_history
    (
        tenant_id,
        job_namespace,
        job_name
    )
    WHERE outcome IS NULL
        AND lease_acquired_at_utc IS NOT NULL;

COMMENT ON COLUMN jobguardian_execution_history.lease_acquired_at_utc
IS 'UTC timestamp when this execution acquired its lease; used to distinguish abandoned work from pre-acquisition requests';

COMMENT ON COLUMN jobguardian_execution_history.outcome
IS 'Terminal outcome: Succeeded, Failed, Cancelled, LeaseLost, Skipped or Abandoned';

COMMENT ON COLUMN jobguardian_execution_history.outcome_reason
IS 'Stable reason code, for example LeaseBusy, Blocked or LeaseExpiredWithoutOutcome';
