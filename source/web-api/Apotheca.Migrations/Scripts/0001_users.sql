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

-- Tracks every Firebase identity linked to an application user.
-- Multiple Firebase UIDs (across providers) can map to the same email.
CREATE TABLE IF NOT EXISTS user_firebase_identities (
    firebase_uid  TEXT        NOT NULL,
    user_id       TEXT        NOT NULL,
    provider_id   TEXT        NOT NULL,   -- e.g. 'google.com', 'microsoft.com', 'password'
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT pk_user_firebase_identities PRIMARY KEY (firebase_uid),
    CONSTRAINT fk_user_firebase_identities_user FOREIGN KEY (user_id) REFERENCES users (id)
);

CREATE INDEX IF NOT EXISTS ix_user_firebase_identities_user_id ON user_firebase_identities (user_id);
