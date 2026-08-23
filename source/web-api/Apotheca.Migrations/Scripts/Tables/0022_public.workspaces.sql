CREATE TABLE IF NOT EXISTS workspaces (
    id              TEXT        NOT NULL,
    "name"          TEXT        NOT NULL,
    plan            TEXT        NOT NULL DEFAULT 'FREE',    -- 'FREE' | 'PAID'
    billing_status  TEXT        NOT NULL DEFAULT 'ACTIVE',  -- 'ACTIVE' | 'PAST_DUE'
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT pk_workspaces PRIMARY KEY (id)
);
