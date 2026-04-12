CREATE TABLE IF NOT EXISTS audit.note_logs (
    id          BIGSERIAL   NOT NULL,
    note_id     TEXT        NOT NULL,
    changed_by  TEXT        NOT NULL,
    changed_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    operation   TEXT        NOT NULL,   -- e.g. 'INSERT', 'UPDATE', 'DELETE'
    log_message TEXT        NOT NULL,   -- e.g. 'Note created', 'Note title updated'
    old_data    JSONB       NULL,
    new_data    JSONB       NULL,

    CONSTRAINT pk_audit_note_logs PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_audit_note_logs_note_id    ON audit.note_logs (note_id);
CREATE INDEX IF NOT EXISTS ix_audit_note_logs_changed_at ON audit.note_logs (changed_at);
