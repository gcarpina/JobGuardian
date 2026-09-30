CREATE TABLE jobguardian_active_executions
(
    tenant_id                  VARCHAR(100) NOT NULL,

    job_namespace              VARCHAR(200) NOT NULL,
    job_name                   VARCHAR(200) NOT NULL,

    execution_id               UUID NOT NULL,

    owner_id                   VARCHAR(500) NOT NULL,

    acquired_at_utc            TIMESTAMPTZ NOT NULL,
    renewed_at_utc             TIMESTAMPTZ NOT NULL,
    lease_until_utc            TIMESTAMPTZ NOT NULL,

    CONSTRAINT pk_jobguardian_active_executions
        PRIMARY KEY
        (
            tenant_id,
            job_namespace,
            job_name
        )
);

COMMENT ON TABLE jobguardian_active_executions
IS 'Current active executions and lease ownership';

COMMENT ON COLUMN jobguardian_active_executions.tenant_id
IS 'Logical tenant identifier';

COMMENT ON COLUMN jobguardian_active_executions.job_namespace
IS 'Logical namespace grouping related jobs';

COMMENT ON COLUMN jobguardian_active_executions.job_name
IS 'Logical job name';

COMMENT ON COLUMN jobguardian_active_executions.execution_id
IS 'Current active execution identifier';

COMMENT ON COLUMN jobguardian_active_executions.owner_id
IS 'Execution owner using environment/application/instance format';

COMMENT ON COLUMN jobguardian_active_executions.acquired_at_utc
IS 'Database UTC timestamp when ownership was acquired';

COMMENT ON COLUMN jobguardian_active_executions.renewed_at_utc
IS 'Database UTC timestamp of the latest heartbeat';

COMMENT ON COLUMN jobguardian_active_executions.lease_until_utc
IS 'Lease expiration timestamp based on database UTC time';

CREATE TABLE jobguardian_job_state
(
    tenant_id                  VARCHAR(100) NOT NULL,

    job_namespace              VARCHAR(200) NOT NULL,
    job_name                   VARCHAR(200) NOT NULL,

    state                      VARCHAR(50) NOT NULL,

    updated_at_utc             TIMESTAMPTZ NOT NULL,

    CONSTRAINT pk_jobguardian_job_state
        PRIMARY KEY
        (
            tenant_id,
            job_namespace,
            job_name
        )
);

COMMENT ON TABLE jobguardian_job_state
IS 'Persistent runtime state for JobGuardian jobs';

COMMENT ON COLUMN jobguardian_job_state.tenant_id
IS 'Logical tenant identifier';

COMMENT ON COLUMN jobguardian_job_state.job_namespace
IS 'Logical namespace grouping related jobs';

COMMENT ON COLUMN jobguardian_job_state.job_name
IS 'Logical job name';

COMMENT ON COLUMN jobguardian_job_state.state
IS 'Persisted non-default runtime state';

COMMENT ON COLUMN jobguardian_job_state.updated_at_utc
IS 'Database UTC timestamp of the latest state update';

CREATE TABLE jobguardian_execution_history
(
    execution_id               UUID PRIMARY KEY,

    tenant_id                  VARCHAR(100) NOT NULL,

    correlation_id             UUID NULL,
    conversation_id            UUID NULL,

    protocol_version           VARCHAR(20) NOT NULL,
    schema_version             VARCHAR(20) NOT NULL,

    job_namespace              VARCHAR(200) NOT NULL,
    job_name                   VARCHAR(200) NOT NULL,

    application_name           VARCHAR(200) NOT NULL,
    application_version        VARCHAR(100) NULL,

    environment                VARCHAR(100) NOT NULL,
    cluster_name               VARCHAR(100) NULL,

    owner_id                   VARCHAR(500) NOT NULL,

    started_at_utc             TIMESTAMPTZ NOT NULL,
    ended_at_utc               TIMESTAMPTZ NULL,

    outcome                    VARCHAR(50) NULL,

    failure_category           VARCHAR(50) NULL,

    run_type                   VARCHAR(50) NOT NULL,
    triggered_by               VARCHAR(50) NOT NULL,

    error_message              TEXT NULL,

    execution_metadata         JSONB NULL
);

COMMENT ON TABLE jobguardian_execution_history
IS 'Complete execution history for JobGuardian jobs';

COMMENT ON COLUMN jobguardian_execution_history.execution_id
IS 'Execution identifier, preferably UUID v7';

COMMENT ON COLUMN jobguardian_execution_history.tenant_id
IS 'Logical tenant identifier';

COMMENT ON COLUMN jobguardian_execution_history.correlation_id
IS 'Correlation identifier used for tracing a single execution flow';

COMMENT ON COLUMN jobguardian_execution_history.conversation_id
IS 'Conversation identifier grouping related executions';

COMMENT ON COLUMN jobguardian_execution_history.protocol_version
IS 'JobGuardian protocol version used by the SDK';

COMMENT ON COLUMN jobguardian_execution_history.schema_version
IS 'Database schema version used at execution time';

COMMENT ON COLUMN jobguardian_execution_history.job_namespace
IS 'Logical namespace grouping related jobs';

COMMENT ON COLUMN jobguardian_execution_history.job_name
IS 'Logical job name';

COMMENT ON COLUMN jobguardian_execution_history.application_name
IS 'Application hosting the execution';

COMMENT ON COLUMN jobguardian_execution_history.application_version
IS 'Application version';

COMMENT ON COLUMN jobguardian_execution_history.environment
IS 'Execution environment such as dev, test, uat or prod';

COMMENT ON COLUMN jobguardian_execution_history.cluster_name
IS 'Cluster identifier when applicable';

COMMENT ON COLUMN jobguardian_execution_history.owner_id
IS 'Execution owner using environment/application/instance format';

COMMENT ON COLUMN jobguardian_execution_history.started_at_utc
IS 'UTC timestamp when the execution attempt started';

COMMENT ON COLUMN jobguardian_execution_history.ended_at_utc
IS 'UTC timestamp when the execution attempt ended';

COMMENT ON COLUMN jobguardian_execution_history.outcome
IS 'ExecutionOutcome value: Succeeded, Failed, Cancelled, LeaseLost or Skipped';

COMMENT ON COLUMN jobguardian_execution_history.failure_category
IS 'ApplicationError, LeaseLost, Cancelled or InfrastructureFailure';

COMMENT ON COLUMN jobguardian_execution_history.run_type
IS 'Scheduled, Manual or Recovery';

COMMENT ON COLUMN jobguardian_execution_history.triggered_by
IS 'Scheduler, Operator, RecoveryPolicy or Api';

COMMENT ON COLUMN jobguardian_execution_history.error_message
IS 'Human readable error description';

COMMENT ON COLUMN jobguardian_execution_history.execution_metadata
IS 'Implementation specific JSON metadata';


CREATE TABLE jobguardian_audit_log
(
    audit_id                   UUID PRIMARY KEY,

    tenant_id                  VARCHAR(100) NOT NULL,

    timestamp_utc              TIMESTAMPTZ NOT NULL,

    user_id                    VARCHAR(200) NOT NULL,
    user_name                  VARCHAR(500) NOT NULL,

    action                     VARCHAR(100) NOT NULL,

    job_namespace              VARCHAR(200) NULL,
    job_name                   VARCHAR(200) NULL,

    execution_id               UUID NULL,

    details                    JSONB NULL
);

COMMENT ON TABLE jobguardian_audit_log
IS 'Administrative and operational audit trail';

COMMENT ON COLUMN jobguardian_audit_log.audit_id
IS 'Audit entry identifier';

COMMENT ON COLUMN jobguardian_audit_log.tenant_id
IS 'Logical tenant identifier';

COMMENT ON COLUMN jobguardian_audit_log.timestamp_utc
IS 'Database UTC timestamp when the action occurred';

COMMENT ON COLUMN jobguardian_audit_log.user_id
IS 'Authenticated user identifier';

COMMENT ON COLUMN jobguardian_audit_log.user_name
IS 'Authenticated user display name';

COMMENT ON COLUMN jobguardian_audit_log.action
IS 'RunNow, ResetFailureState, ForceReleaseLease, ForceTakeoverLease or other administrative action';

COMMENT ON COLUMN jobguardian_audit_log.job_namespace
IS 'Affected job namespace';

COMMENT ON COLUMN jobguardian_audit_log.job_name
IS 'Affected job name';

COMMENT ON COLUMN jobguardian_audit_log.execution_id
IS 'Affected execution identifier, if applicable';

COMMENT ON COLUMN jobguardian_audit_log.details
IS 'Action specific JSON payload';


CREATE INDEX ix_jgh_job
ON jobguardian_execution_history
(
    tenant_id,
    job_namespace,
    job_name
);

CREATE INDEX ix_jgh_started_at
ON jobguardian_execution_history
(
    started_at_utc DESC
);

CREATE INDEX ix_jgh_outcome
ON jobguardian_execution_history
(
    outcome
);

CREATE INDEX ix_jgh_correlation
ON jobguardian_execution_history
(
    correlation_id
);

CREATE INDEX ix_jgh_conversation
ON jobguardian_execution_history
(
    conversation_id
);

CREATE INDEX ix_jgal_timestamp
ON jobguardian_audit_log
(
    timestamp_utc DESC
);

CREATE INDEX ix_jgal_user
ON jobguardian_audit_log
(
    user_id
);