-- Создание пользователя, если его ещё нет
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_user WHERE usename = 'exam_paper_docker') THEN
        CREATE USER exam_paper_docker WITH PASSWORD '1234';
    END IF;
END
$$;

-- Назначить привилегии
GRANT CONNECT ON DATABASE exam_paper TO exam_paper_docker;
GRANT USAGE ON SCHEMA public TO exam_paper_docker;
GRANT CREATE ON SCHEMA public TO exam_paper_docker;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO exam_paper_docker;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO exam_paper_docker;

-- Опциональные расширения
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;
