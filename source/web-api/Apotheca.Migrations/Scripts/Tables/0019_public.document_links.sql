CREATE TABLE IF NOT EXISTS document_links (
    id          TEXT        NOT NULL,
    document_id TEXT        NOT NULL,
    created_by  TEXT        NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT pk_document_links             PRIMARY KEY (id),
    CONSTRAINT fk_document_links_document    FOREIGN KEY (document_id) REFERENCES documents (id),
    CONSTRAINT fk_document_links_created_by  FOREIGN KEY (created_by)  REFERENCES users (id)
);

CREATE INDEX IF NOT EXISTS ix_document_links_document_id ON document_links (document_id);
