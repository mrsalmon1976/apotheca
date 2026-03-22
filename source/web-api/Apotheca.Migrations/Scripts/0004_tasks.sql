CREATE TABLE IF NOT EXISTS tasks (
    id              TEXT        NOT NULL,
    project_id      TEXT        NOT NULL,
    parent_task_id  TEXT        NULL,
    title           TEXT        NOT NULL,
    notes           TEXT        NULL,
    assigned_to     TEXT        NULL,
    created_by      TEXT        NOT NULL,
    priority        TEXT        NOT NULL DEFAULT 'NONE',   -- 'none', 'low', 'medium', 'high', 'urgent'
    due_at          TIMESTAMPTZ NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT pk_tasks                 PRIMARY KEY (id),
    CONSTRAINT fk_tasks_project         FOREIGN KEY (project_id)     REFERENCES projects (id),
    CONSTRAINT fk_tasks_parent          FOREIGN KEY (parent_task_id) REFERENCES tasks (id),
    CONSTRAINT fk_tasks_assigned_to     FOREIGN KEY (assigned_to)    REFERENCES users (id),
    CONSTRAINT fk_tasks_created_by      FOREIGN KEY (created_by)     REFERENCES users (id),
    CONSTRAINT ck_tasks_priority        CHECK (priority IN ('NONE', 'LOW', 'MEDIUM', 'HIGH', 'URGENT'))
);

CREATE INDEX IF NOT EXISTS ix_tasks_project_id      ON tasks (project_id);
CREATE INDEX IF NOT EXISTS ix_tasks_parent_task_id  ON tasks (parent_task_id);
CREATE INDEX IF NOT EXISTS ix_tasks_assigned_to     ON tasks (assigned_to);
CREATE INDEX IF NOT EXISTS ix_tasks_due_at          ON tasks (due_at);
