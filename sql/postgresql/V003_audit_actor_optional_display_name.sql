ALTER TABLE jobguardian_audit_log
ALTER COLUMN user_name DROP NOT NULL;

COMMENT ON TABLE jobguardian_audit_log
IS 'Audit records for supported JobGuardian administrative requests';

COMMENT ON COLUMN jobguardian_audit_log.user_name
IS 'Actor display name when supplied by the hosting application';

COMMENT ON COLUMN jobguardian_audit_log.action
IS 'ResetJobRequested for programmatic SDK reset requests';
