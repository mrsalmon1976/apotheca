CREATE TABLE IF NOT EXISTS workspace_users (
    workspace_id    TEXT        NOT NULL,
    user_id         TEXT        NOT NULL,
    workspace_role  TEXT        NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT pk_workspace_users PRIMARY KEY (workspace_id, user_id),
    CONSTRAINT fk_workspace_users_workspace FOREIGN KEY (workspace_id) REFERENCES workspaces (id),
    CONSTRAINT fk_workspace_users_user      FOREIGN KEY (user_id)      REFERENCES users (id)
);

CREATE INDEX IF NOT EXISTS ix_workspace_users_workspace_id ON workspace_users (workspace_id);
CREATE INDEX IF NOT EXISTS ix_workspace_users_user_id      ON workspace_users (user_id);
