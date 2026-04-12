CREATE TABLE IF NOT EXISTS audit.project_activity_logs (
    id          BIGSERIAL   NOT NULL,
    project_id  TEXT        NOT NULL,
    ref_id      TEXT        NOT NULL,
    log_message TEXT        NOT NULL,
    user_id     TEXT        NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT pk_audit_project_activity_log PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_audit_project_activity_log_project_id ON audit.project_activity_logs (project_id);
CREATE INDEX IF NOT EXISTS ix_audit_project_activity_log_created_at ON audit.project_activity_logs (created_at);
