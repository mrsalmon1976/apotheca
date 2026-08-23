ALTER TABLE IF EXISTS user_projects RENAME TO project_users;

CREATE TABLE IF NOT EXISTS project_users (
    user_id         TEXT        NOT NULL,
    project_id      TEXT        NOT NULL,
    project_role    TEXT        NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT pk_project_users PRIMARY KEY (user_id, project_id),
    CONSTRAINT fk_project_users_user    FOREIGN KEY (user_id)    REFERENCES users (id),
    CONSTRAINT fk_project_users_project FOREIGN KEY (project_id) REFERENCES projects (id)
);

ALTER INDEX IF EXISTS ix_user_projects_user_id    RENAME TO ix_project_users_user_id;
ALTER INDEX IF EXISTS ix_user_projects_project_id RENAME TO ix_project_users_project_id;

CREATE INDEX IF NOT EXISTS ix_project_users_user_id    ON project_users (user_id);
CREATE INDEX IF NOT EXISTS ix_project_users_project_id ON project_users (project_id);

-- user_projects was renamed to project_users for consistency with workspace_users.
-- Idempotent: once user_projects is gone, this is a no-op on rerun.

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'pk_user_projects') THEN
        ALTER TABLE project_users RENAME CONSTRAINT pk_user_projects TO pk_project_users;
    END IF;
    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_user_projects_user') THEN
        ALTER TABLE project_users RENAME CONSTRAINT fk_user_projects_user TO fk_project_users_user;
    END IF;
    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_user_projects_project') THEN
        ALTER TABLE project_users RENAME CONSTRAINT fk_user_projects_project TO fk_project_users_project;
    END IF;
END;
$$;

-- DataConstants.ProjectRole was renamed: Owner -> Admin, User -> Contributor (Viewer unchanged).
-- Idempotent: once the old values are gone, these are no-ops on rerun.
UPDATE project_users SET project_role = 'ADMIN'       WHERE project_role = 'OWNER';
UPDATE project_users SET project_role = 'CONTRIBUTOR' WHERE project_role = 'USER';


