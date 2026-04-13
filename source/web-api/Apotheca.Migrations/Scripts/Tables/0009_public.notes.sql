CREATE TABLE IF NOT EXISTS notes (
    id              TEXT        NOT NULL,
    project_id      TEXT        NOT NULL,
    parent_note_id  TEXT        NULL,
    is_folder		BOOLEAN     NOT NULL,
    title           TEXT        NOT NULL,
    body            TEXT        NULL,
    created_by      TEXT        NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT pk_notes                 PRIMARY KEY (id),
    CONSTRAINT fk_notes_project         FOREIGN KEY (project_id)     REFERENCES projects (id),
    CONSTRAINT fk_notes_parent          FOREIGN KEY (parent_note_id) REFERENCES notes (id),
    CONSTRAINT fk_notes_created_by      FOREIGN KEY (created_by)     REFERENCES users (id)
);

CREATE INDEX IF NOT EXISTS ix_notes_project_id      ON notes (project_id);
CREATE INDEX IF NOT EXISTS ix_notes_parent_note_id  ON notes (parent_note_id);

ALTER TABLE notes ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ NULL;
