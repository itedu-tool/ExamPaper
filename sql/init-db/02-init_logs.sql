CREATE SCHEMA IF NOT EXISTS audit;

REVOKE ALL ON SCHEMA audit FROM public;

CREATE TABLE audit.ddl_log
(
    id                BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    event_tstamp      TIMESTAMP WITH TIME ZONE DEFAULT current_timestamp NOT NULL,
    action            TEXT                                               NOT NULL,
    object_type       TEXT,
    object_identity   TEXT,
    session_user_name TEXT                     DEFAULT session_user,
    client_query      TEXT                     DEFAULT current_query()
);

CREATE INDEX idx_audit_ddl_log_tstamp ON audit.ddl_log (event_tstamp);


CREATE TABLE audit.log
(
    id                BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    action_tstamp     TIMESTAMP WITH TIME ZONE DEFAULT current_timestamp NOT NULL,
    action            TEXT                                               NOT NULL CHECK (action IN ('INSERT', 'DELETE', 'UPDATE', 'TRUNCATE')),
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
    RETURNS trigger AS
$body$
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
        VALUES (TG_TABLE_SCHEMA::text, TG_TABLE_NAME::text, 'UPDATE', v_old_data, v_new_data);

        RETURN NEW;

    ELSIF (TG_OP = 'DELETE') THEN
        v_old_data := to_jsonb(OLD);

        INSERT INTO audit.log (table_schema, table_name, action, old_data)
        VALUES (TG_TABLE_SCHEMA::text, TG_TABLE_NAME::text, 'DELETE', v_old_data);

        RETURN OLD;

    ELSIF (TG_OP = 'INSERT') THEN
        v_new_data := to_jsonb(NEW);

        INSERT INTO audit.log (table_schema, table_name, action, new_data)
        VALUES (TG_TABLE_SCHEMA::text, TG_TABLE_NAME::text, 'INSERT', v_new_data);

        RETURN NEW;

    ELSIF (TG_OP = 'TRUNCATE') THEN
        INSERT INTO audit.log (table_schema, table_name, action)
        VALUES (TG_TABLE_SCHEMA::text, TG_TABLE_NAME::text, 'TRUNCATE');
        RETURN NULL;
    END IF;

    RETURN NULL;
END;
$body$
    LANGUAGE plpgsql
    SECURITY DEFINER
    SET search_path = pg_temp;



CREATE OR REPLACE FUNCTION audit.log_ddl_commands()
    RETURNS event_trigger AS
$body$
DECLARE
    action_record RECORD;
BEGIN

    FOR action_record IN SELECT * FROM pg_event_trigger_ddl_commands()
        LOOP
            INSERT INTO audit.ddl_log (action,
                                       object_type,
                                       object_identity)
            VALUES (TG_TAG,
                    action_record.object_type,
                    action_record.object_identity);
        END LOOP;
END;
$body$ LANGUAGE plpgsql
    SECURITY DEFINER
    SET search_path = pg_temp;


CREATE OR REPLACE FUNCTION audit.log_ddl_drop_commands()
    RETURNS event_trigger AS
$$
DECLARE
    action_record RECORD;
BEGIN
    FOR action_record IN SELECT * FROM pg_event_trigger_dropped_objects()
        LOOP
            IF action_record.object_type = 'table' THEN
                INSERT INTO audit.ddl_log (action,
                                           object_type,
                                           object_identity)
                VALUES (TG_TAG,
                        action_record.object_type,
                        action_record.object_identity);
            END IF;
        END LOOP;
END;
$$ LANGUAGE plpgsql
    SECURITY DEFINER
    SET search_path = pg_temp;
