CREATE TABLE IF NOT EXISTS tickets (
    id                TEXT        NOT NULL,
    project_id        TEXT        NOT NULL,
    parent_ticket_id  TEXT        NULL,
    ticket_type		  TEXT        NOT NULL,   -- e.g. 'bug', 'feature', 'task'    
    title             TEXT        NOT NULL,
    body              TEXT        NULL,
    created_by        TEXT        NOT NULL,
    priority          TEXT        NOT NULL DEFAULT 'NONE',   -- 'none', 'low', 'medium', 'high', 'urgent'
    assigned_to       TEXT        NULL,
    due_at            TIMESTAMPTZ NULL,
    created_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    completed_at      TIMESTAMPTZ NULL,

    CONSTRAINT pk_tickets                 PRIMARY KEY (id),
    CONSTRAINT fk_tickets_project         FOREIGN KEY (project_id)     REFERENCES projects (id),
    CONSTRAINT fk_tickets_parent          FOREIGN KEY (parent_ticket_id) REFERENCES tickets (id),
    CONSTRAINT fk_tickets_created_by      FOREIGN KEY (created_by)     REFERENCES users (id),
    CONSTRAINT fk_tickets_assigned_to     FOREIGN KEY (assigned_to)     REFERENCES users (id)
);

CREATE INDEX IF NOT EXISTS ix_tickets_project_id        ON tickets (project_id);
CREATE INDEX IF NOT EXISTS ix_tickets_parent_ticket_id  ON tickets (parent_ticket_id);
