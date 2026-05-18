import apiClient from './client';
import type {GenerationSettings, ExamGenerationResult} from '@/types/exam';

export const ExamsApi = {
  generate(settings: GenerationSettings): Promise<ExamGenerationResult> {
    return apiClient.post('/exam/generate', settings).then(res => res.data);
  },
  async exportPdf(settings: GenerationSettings): Promise<void> {
    const response = await apiClient.post('/exam/export/pdf', settings, {
      responseType: 'blob',
    });
    // Скачиваем файл
    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', 'exam.pdf');
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  },
  async exportJson(settings: GenerationSettings): Promise<void> {
    const response = await apiClient.post('/exam/export/json', settings, {
      responseType: 'blob',
    });
    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', 'exam.json');
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  }
};
