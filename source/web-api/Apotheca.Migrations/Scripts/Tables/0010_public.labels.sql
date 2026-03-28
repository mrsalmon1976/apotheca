CREATE TABLE IF NOT EXISTS labels (
    id          TEXT        NOT NULL,
    project_id  TEXT        NOT NULL,
    label_text  TEXT        NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by  TEXT        NOT NULL,

    CONSTRAINT pk_labels                PRIMARY KEY (id),
    CONSTRAINT fk_labels_project        FOREIGN KEY (project_id) REFERENCES projects (id),
    CONSTRAINT fk_labels_created_by     FOREIGN KEY (created_by) REFERENCES users (id),
    CONSTRAINT uq_labels_project_text   UNIQUE (project_id, label_text)
);

CREATE INDEX IF NOT EXISTS ix_labels_project_id ON labels (project_id);

