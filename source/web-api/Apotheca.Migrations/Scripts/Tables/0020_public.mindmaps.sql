CREATE TABLE IF NOT EXISTS mindmaps (
    id          TEXT        NOT NULL,
    project_id  TEXT        NOT NULL,
    name        TEXT        NOT NULL DEFAULT 'Untitle Mindmap',
    created_by  TEXT        NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at  TIMESTAMPTZ NULL,

    CONSTRAINT pk_mindmaps            PRIMARY KEY (id),
    CONSTRAINT fk_mindmaps_project    FOREIGN KEY (project_id) REFERENCES projects (id),
    CONSTRAINT fk_mindmaps_created_by FOREIGN KEY (created_by) REFERENCES users (id)
);

CREATE INDEX IF NOT EXISTS ix_mindmaps_project_id ON mindmaps (project_id);

ALTER TABLE mindmaps ADD COLUMN IF NOT EXISTS name TEXT NOT NULL DEFAULT 'Untitle Mindmap';
