import type {QuestionDto} from './question';

export interface TicketDto {
  ticketName: string;
  questions: QuestionDto[];
}

export interface ExamGenerationResult {
  totalTicketsCount: number;
  questionsPerTicketCount: number;
  ticketNameTemplate: string;
  tickets: TicketDto[];
}

// Настройки, отправляемые на сервер
export interface GenerationSettings {
  totalTicketsCount: number;
  questionsPerTicketCount: number;
  ticketNameTemplate: string;
}
