<template>
  <div class="questions-list">
    <h2>Вопросы</h2>
    <router-link to="/questions/new" class="btn btn-primary">Создать вопрос</router-link>
    <div v-if="loading">Загрузка...</div>
    <ul v-else-if="questions.length">
      <li v-for="q in questions" :key="q.id">
        <span>{{ q.text }}</span>
        <button @click="deleteQuestion(q.id)" :disabled="deleting === q.id">Удалить</button>
      </li>
    </ul>
    <p v-else>Вопросов пока нет</p>
  </div>
</template>

<script setup lang="ts">
import {ref, onMounted} from 'vue';
import {QuestionsApi} from '@/api/questions';
import type {QuestionDto} from '@/types/question';

const questions = ref<QuestionDto[]>([]);
const loading = ref(true);
const deleting = ref<string | null>(null);

onMounted(async () => {
  try {
    questions.value = await QuestionsApi.getAll();
  } catch (e) {
    console.error(e);
  } finally {
    loading.value = false;
  }
});

async function deleteQuestion(id: string) {
  if (!confirm('Удалить вопрос?')) return;
  deleting.value = id;
  try {
    await QuestionsApi.delete(id);
    questions.value = questions.value.filter(q => q.id !== id);
  } catch (e) {
    console.error(e);
  } finally {
    deleting.value = null;
  }
}
</script>
