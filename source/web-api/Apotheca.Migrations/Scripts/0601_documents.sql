CREATE TABLE IF NOT EXISTS documents (
    id              TEXT        NOT NULL,
    project_id      TEXT        NOT NULL,
    is_folder		BOOLEAN     NOT NULL,
    title           TEXT        NOT NULL,
    file_name       TEXT        NULL,
    file_extension  TEXT        NULL,
    mimetype        TEXT        NULL,
    file_length     BIGINT      NULL,
    blob_reference  TEXT        NULL,
    created_by      TEXT        NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT pk_documents                 PRIMARY KEY (id),
    CONSTRAINT fk_documents_project         FOREIGN KEY (project_id)     REFERENCES projects (id),
    CONSTRAINT fk_documents_created_by      FOREIGN KEY (created_by)     REFERENCES users (id)
);

CREATE INDEX IF NOT EXISTS ix_documents_project_id      ON documents (project_id);
