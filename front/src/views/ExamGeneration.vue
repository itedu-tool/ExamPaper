<template>
  <div>
    <h2>Генерация билетов</h2>
    <form @submit.prevent="generate">
      <label>Количество билетов: <input type="number" v-model.number="settings.totalTicketsCount" min="1"
                                        required/></label>
      <label>Вопросов в билете: <input type="number" v-model.number="settings.questionsPerTicketCount" min="1"
                                       required/></label>
      <label>Шаблон названия (например, "Билет {0}"): <input v-model="settings.ticketNameTemplate" required/></label>
      <button type="submit" :disabled="loading">Сгенерировать</button>
    </form>
    <div v-if="loading">Генерация...</div>
    <div v-if="result" class="result">
      <h3>Результат ({{ result.totalTicketsCount }} билетов)</h3>
      <div v-for="ticket in result.tickets" :key="ticket.ticketName" class="ticket">
        <strong>{{ ticket.ticketName }}</strong>
        <ol>
          <li v-for="q in ticket.questions" :key="q.id">{{ q.text }}</li>
        </ol>
      </div>
    </div>
    <div v-if="result" class="export-actions">
      <button @click="exportPdf">Сохранить PDF</button>
      <button @click="exportJson">Сохранить JSON</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import {reactive, ref} from 'vue';
import {ExamsApi} from '@/api/exams';
import type {GenerationSettings, ExamGenerationResult} from '@/types/exam';

const settings = reactive<GenerationSettings>({
  totalTicketsCount: 3,
  questionsPerTicketCount: 5,
  ticketNameTemplate: 'Билет {0}',
});
const loading = ref(false);
const result = ref<ExamGenerationResult | null>(null);

async function generate() {
  loading.value = true;
  result.value = null;
  try {
    result.value = await ExamsApi.generate(settings);
  } catch (e) {
    console.error(e);
    alert('Ошибка генерации');
  } finally {
    loading.value = false;
  }
}

async function exportPdf() {
  try {
    await ExamsApi.exportPdf(settings); // используем те же настройки
  } catch (e) {
    alert('Ошибка экспорта PDF');
  }
}

async function exportJson() {
  try {
    await ExamsApi.exportJson(settings);
  } catch (e) {
    alert('Ошибка экспорта JSON');
  }
}
</script>
