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
