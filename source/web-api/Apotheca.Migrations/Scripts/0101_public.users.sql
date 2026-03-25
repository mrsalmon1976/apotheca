CREATE TABLE IF NOT EXISTS users (
    id            TEXT        NOT NULL,
    email         TEXT        NOT NULL,
    display_name  TEXT        NOT NULL,
    photo_url     TEXT        NULL,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT pk_users PRIMARY KEY (id),
    CONSTRAINT uq_users_email UNIQUE (email)
);

CREATE INDEX IF NOT EXISTS ix_users_email ON users (email);
