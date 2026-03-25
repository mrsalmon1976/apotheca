CREATE TABLE IF NOT EXISTS projects (
    id          TEXT        NOT NULL,
    title       TEXT        NOT NULL,
    summary     TEXT        NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT pk_projects PRIMARY KEY (id)
);
