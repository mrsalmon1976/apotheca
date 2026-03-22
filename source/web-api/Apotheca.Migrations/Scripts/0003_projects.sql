CREATE TABLE IF NOT EXISTS projects (
    id          TEXT      NOT NULL,
    name        TEXT        NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT pk_projects PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS user_projects (
    user_id         TEXT        NOT NULL,
    project_id      TEXT        NOT NULL,
    project_role    TEXT        NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT pk_user_projects PRIMARY KEY (user_id, project_id),
    CONSTRAINT fk_user_projects_user    FOREIGN KEY (user_id)    REFERENCES users (id),
    CONSTRAINT fk_user_projects_project FOREIGN KEY (project_id) REFERENCES projects (id)
);

CREATE INDEX IF NOT EXISTS ix_user_projects_user_id    ON user_projects (user_id);
CREATE INDEX IF NOT EXISTS ix_user_projects_project_id ON user_projects (project_id);

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
