CREATE TABLE IF NOT EXISTS documents (
    id                  TEXT        NOT NULL,
    project_id          TEXT        NOT NULL,
    parent_document_id  TEXT        NULL,
    is_folder           BOOLEAN     NOT NULL,
    title               TEXT        NOT NULL,
    file_name           TEXT        NULL,
    file_extension      TEXT        NULL,
    mimetype            TEXT        NULL,
    file_length         BIGINT      NULL,
    blob_reference      TEXT        NULL,
    created_by          TEXT        NOT NULL,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at          TIMESTAMPTZ NULL,

    CONSTRAINT pk_documents                 PRIMARY KEY (id),
    CONSTRAINT fk_documents_project         FOREIGN KEY (project_id)            REFERENCES projects (id),
    CONSTRAINT fk_documents_parent          FOREIGN KEY (parent_document_id)    REFERENCES documents (id),
    CONSTRAINT fk_documents_created_by      FOREIGN KEY (created_by)            REFERENCES users (id)
);

CREATE INDEX IF NOT EXISTS ix_documents_project_id          ON documents (project_id);
CREATE INDEX IF NOT EXISTS ix_documents_parent_document_id  ON documents (parent_document_id);

ALTER TABLE documents ADD COLUMN IF NOT EXISTS parent_document_id TEXT NULL;
ALTER TABLE documents ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'fk_documents_parent' AND table_name = 'documents'
    ) THEN
        ALTER TABLE documents ADD CONSTRAINT fk_documents_parent FOREIGN KEY (parent_document_id) REFERENCES documents (id);
    END IF;
END$$;

CREATE INDEX IF NOT EXISTS ix_documents_parent_document_id  ON documents (parent_document_id);
