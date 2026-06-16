classDiagram
direction BT
class table_exam_paper_questions {
   uuid exam_paper_id
   uuid question_id
}
class table_exam_papers {
   uuid generation_setting_id
   text title
   uuid id
}
class table_generation_settings {
   integer total_tickets_count
   integer oral_questions_per_ticket
   integer practical_questions_per_ticket
   text ticket_name_template
   uuid id
}
class table_question_tags {
   uuid question_id
   uuid tag_id
}
class table_questions {
   text text
   enum_question_type type
   uuid id
}
class table_tags {
   text name
   uuid id
}

table_exam_paper_questions  -->  table_exam_papers : exam_paper_id:id
table_exam_paper_questions  -->  table_questions : question_id:id
table_exam_papers  -->  table_generation_settings : generation_setting_id:id
table_question_tags  -->  table_questions : question_id:id
table_question_tags  -->  table_tags : tag_id:id
