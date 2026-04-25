CREATE TABLE IF NOT EXISTS audit.document_logs (
    id              BIGSERIAL   NOT NULL,
    document_id     TEXT        NOT NULL,
    changed_by      TEXT        NOT NULL,
    changed_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    operation       TEXT        NOT NULL,   -- e.g. 'INSERT', 'UPDATE', 'DELETE'
    log_message     TEXT        NOT NULL,   -- e.g. 'Document created', 'Document title updated'
    old_data        JSONB       NULL,
    new_data        JSONB       NULL,

    CONSTRAINT pk_audit_document_logs PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_audit_document_logs_document_id ON audit.document_logs (document_id);
CREATE INDEX IF NOT EXISTS ix_audit_document_logs_changed_at  ON audit.document_logs (changed_at);
