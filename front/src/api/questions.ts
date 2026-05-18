import apiClient from './client';
import type {QuestionDto, CreateQuestionDto} from '@/types/question';

export const QuestionsApi = {
  getAll(): Promise<QuestionDto[]> {
    return apiClient.get('/questions').then(res => res.data);
  },
  getById(id: string): Promise<QuestionDto> {
    return apiClient.get(`/questions/${id}`).then(res => res.data);
  },
  create(dto: CreateQuestionDto): Promise<QuestionDto> {
    return apiClient.post('/questions', dto).then(res => res.data);
  },
  delete(id: string): Promise<void> {
    return apiClient.delete(`/questions/${id}`);
  }
};
