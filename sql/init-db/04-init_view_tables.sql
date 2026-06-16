CREATE OR REPLACE VIEW view_exam_papers_detailed AS
SELECT ep.id    AS exam_paper_id,
       ep.title AS exam_paper_title,
       gs.total_tickets_count,
       gs.oral_questions_per_ticket,
       gs.practical_questions_per_ticket,
       gs.ticket_name_template

FROM table_exam_papers AS ep
         JOIN public.table_generation_settings AS gs ON ep.generation_setting_id = gs.id;



CREATE OR REPLACE VIEW view_exam_questions AS
SELECT ep.id    AS exam_paper_id,
       ep.title AS exam_paper_title,
       q.text   AS question_text,
       q.type   AS question_type

FROM table_exam_paper_questions AS epq
         JOIN table_exam_papers AS ep ON epq.exam_paper_id = ep.id
         JOIN table_questions AS q ON epq.question_id = q.id;



CREATE OR REPLACE VIEW view_questions_with_tags AS
SELECT q.id                                                                AS question_id,
       q.text                                                              AS question_text,
       q.type                                                              AS question_type,

       COALESCE(ARRAY_AGG(t.name) FILTER (WHERE t.name IS NOT NULL), '{}') AS tags
FROM table_questions AS q
         LEFT JOIN table_question_tags AS qt ON q.id = qt.question_id
         LEFT JOIN table_tags AS t ON qt.tag_id = t.id
GROUP BY q.id, q.text, q.type
