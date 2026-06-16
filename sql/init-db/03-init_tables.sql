CREATE TYPE enum_question_type AS ENUM ('oral', 'practical');

CREATE TABLE IF NOT EXISTS table_questions
(
    id   UUID PRIMARY KEY DEFAULT uuidv7(),
    text TEXT               NOT NUll,
    type enum_question_type NOT NULL,

    CONSTRAINT check_questions_text_length CHECK ( length(text) > 0)
);



CREATE TABLE IF NOT EXISTS table_tags
(
    id   UUID PRIMARY KEY DEFAULT uuidv7(),
    name TEXT NOT NULL,

    CONSTRAINT uq_tags_name UNIQUE (name),
    CONSTRAINT chk_tags_name_length CHECK ( length(name) > 0 )
);


CREATE TABLE IF NOT EXISTS table_question_tags
(
    question_id UUID NOT NULL,
    tag_id      UUID NOT NULL,

    CONSTRAINT pk_question_tags PRIMARY KEY (question_id, tag_id),

    CONSTRAINT fk_question_tags_question
        FOREIGN KEY (question_id)
            REFERENCES table_questions (id) ON DELETE RESTRICT,

    CONSTRAINT fk_question_tags_tag
        FOREIGN KEY (tag_id)
            REFERENCES table_tags (id) ON DELETE RESTRICT

);

CREATE TABLE IF NOT EXISTS table_generation_settings
(
    id                        UUID PRIMARY KEY DEFAULT uuidv7(),
    total_tickets_count       INTEGER NOT NULL,
    oral_questions_per_ticket        INTEGER NOT NULL DEFAULT 0,
    practical_questions_per_ticket   INTEGER NOT NULL DEFAULT 0,
    ticket_name_template      TEXT    NOT NULL
);

CREATE TABLE IF NOT EXISTS table_exam_papers
(
    id                    UUID PRIMARY KEY DEFAULT uuidv7(),
    generation_setting_id UUID NOT NULL,
    title                 TEXT NOT NULL,

    CONSTRAINT check_title_length CHECK ( length(title) > 0),

    CONSTRAINT fk_exam_papers_generation_settings
        FOREIGN KEY (generation_setting_id)
            REFERENCES table_generation_settings (id) ON DELETE RESTRICT
);


CREATE TABLE IF NOT EXISTS table_exam_paper_questions
(
    exam_paper_id UUID NOT NULL,
    question_id   UUID NOT NULL,

    CONSTRAINT pk_exam_paper_questions PRIMARY KEY (exam_paper_id, question_id),

    CONSTRAINT fk_exam_paper_questions_paper
        FOREIGN KEY (exam_paper_id)
            REFERENCES table_exam_papers (id)
            ON DELETE RESTRICT,

    CONSTRAINT fk_exam_paper_questions_question
        FOREIGN KEY (question_id)
            REFERENCES table_questions (id)
            ON DELETE RESTRICT
);
