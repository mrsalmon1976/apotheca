CREATE TABLE IF NOT EXISTS audit.project_logs (
    id          BIGSERIAL   NOT NULL,
    project_id  TEXT        NOT NULL,
    changed_by  TEXT        NOT NULL,
    changed_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    operation   TEXT        NOT NULL,   -- e.g. 'INSERT', 'UPDATE', 'DELETE'
    log_message TEXT     NOT NULL,      -- e.g. 'Project created', 'Project name updated'
    old_data    JSONB       NULL,
    new_data    JSONB       NULL,

    CONSTRAINT pk_audit_project_logs PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_audit_project_logs_project_id ON audit.project_logs (project_id);
CREATE INDEX IF NOT EXISTS ix_audit_project_logs_changed_at ON audit.project_logs (changed_at);
