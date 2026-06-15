CREATE SCHEMA IF NOT EXISTS audit;

REVOKE ALL ON SCHEMA audit FROM public;

CREATE TABLE audit.log
(
    id                BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    action_tstamp     TIMESTAMP WITH TIME ZONE DEFAULT current_timestamp NOT NULL,
    action            TEXT                                               NOT NULL CHECK (action IN ('I', 'D', 'U', 'T')),
    table_schema      TEXT                                               NOT NULL,
    table_name        TEXT                                               NOT NULL,
    session_user_name TEXT                     DEFAULT session_user,
    client_query      TEXT                     DEFAULT current_query(),
    transaction_id    xid8                     DEFAULT pg_current_xact_id(),
    old_data          JSONB,
    new_data          JSONB
);

CREATE INDEX idx_audit_log_table ON audit.log (table_schema, table_name);
CREATE INDEX idx_audit_log_tstamp ON audit.log (action_tstamp);



CREATE OR REPLACE FUNCTION audit.if_modified_func()
    RETURNS trigger AS $body$
DECLARE
    v_old_data jsonb;
    v_new_data jsonb;
BEGIN
    IF (TG_OP = 'UPDATE') THEN
        v_old_data := to_jsonb(OLD);
        v_new_data := to_jsonb(NEW);

        IF v_old_data = v_new_data THEN
            RETURN NEW;
        END IF;

        INSERT INTO audit.log (table_schema, table_name, action, old_data, new_data)
        VALUES (TG_TABLE_SCHEMA::text, TG_TABLE_NAME::text, 'U', v_old_data, v_new_data);

        RETURN NEW;

    ELSIF (TG_OP = 'DELETE') THEN
        v_old_data := to_jsonb(OLD);

        INSERT INTO audit.log (table_schema, table_name, action, old_data)
        VALUES (TG_TABLE_SCHEMA::text, TG_TABLE_NAME::text, 'D', v_old_data);

        RETURN OLD;

    ELSIF (TG_OP = 'INSERT') THEN
        v_new_data := to_jsonb(NEW);

        INSERT INTO audit.log (table_schema, table_name, action, new_data)
        VALUES (TG_TABLE_SCHEMA::text, TG_TABLE_NAME::text, 'I', v_new_data);

        RETURN NEW;
    END IF;

    RETURN NULL;
END;
$body$
    LANGUAGE plpgsql
    SECURITY DEFINER
    SET search_path = pg_temp;
