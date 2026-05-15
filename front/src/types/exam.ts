import type { QuestionDto } from './question';

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

export interface GenerationSettings {
  totalTicketsCount: number;
  questionsPerTicketCount: number;
  ticketNameTemplate: string;
}
