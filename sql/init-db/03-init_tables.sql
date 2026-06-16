CREATE TABLE IF NOT EXISTS questions
(
    id   UUID PRIMARY KEY DEFAULT uuidv7(),
    text TEXT NOT NUll,

    CONSTRAINT check_text_length CHECK ( length(text) > 0)
);


CREATE TABLE IF NOT EXISTS generation_settings
(
    id                        UUID PRIMARY KEY DEFAULT uuidv7(),
    total_tickets_count       INTEGER NOT NULL,
    questions_per_ticketCount INTEGER NOT NULL,
    ticket_name_template      TEXT    NOT NULL
);

CREATE TABLE IF NOT EXISTS exam_papers
(
    id                    UUID PRIMARY KEY DEFAULT uuidv7(),
    generation_setting_id UUID NOT NULL,
    title                 TEXT NOT NULL,

    CONSTRAINT check_title_length CHECK ( length(title) > 0),

    CONSTRAINT fk_exam_papers_generation_settings
        FOREIGN KEY (generation_setting_id)
            REFERENCES generation_settings (id) ON DELETE RESTRICT
);


CREATE TABLE IF NOT EXISTS exam_paper_questions
(
    exam_paper_id UUID NOT NULL,
    question_id   UUID NOT NULL,

    PRIMARY KEY (exam_paper_id, question_id),

    CONSTRAINT fk_exam_paper
        FOREIGN KEY (exam_paper_id)
            REFERENCES exam_papers (id)
            ON DELETE RESTRICT,

    CONSTRAINT fk_question
        FOREIGN KEY (question_id)
            REFERENCES questions (id)
            ON DELETE RESTRICT
);
