CREATE TABLE IF NOT EXISTS user_settings (
    user_id               TEXT        NOT NULL,
    current_workspace_id  TEXT        NULL,
    created_at            TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at            TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT pk_user_settings PRIMARY KEY (user_id),
    CONSTRAINT fk_user_settings_user      FOREIGN KEY (user_id)              REFERENCES users (id),
    CONSTRAINT fk_user_settings_workspace FOREIGN KEY (current_workspace_id) REFERENCES workspaces (id)
);
