CREATE TABLE IF NOT EXISTS audit.user_logs (
    id          TEXT        NOT NULL,
    user_id     TEXT        NOT NULL,
    event_type  TEXT        NOT NULL,
    log_message TEXT        NOT NULL,
    ip_address  TEXT        NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT pk_audit_user_logs PRIMARY KEY (id)
);
