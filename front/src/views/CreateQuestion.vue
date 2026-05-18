<template>
  <div>
    <h2>Новый вопрос</h2>
    <form @submit.prevent="submit">
      <label>
        Текст вопроса:
        <textarea v-model="text" rows="3" required/>
      </label>
      <div v-if="error" class="error">{{ error }}</div>
      <button type="submit" :disabled="saving">Сохранить</button>
      <router-link to="/questions">Отмена</router-link>
    </form>
  </div>
</template>

<script setup lang="ts">
import {ref} from 'vue';
import {useRouter} from 'vue-router';
import {QuestionsApi} from '@/api/questions';

const text = ref('');
const saving = ref(false);
const error = ref('');
const router = useRouter();

async function submit() {
  saving.value = true;
  error.value = '';
  try {
    await QuestionsApi.create({text: text.value});
    router.push('/questions');
  } catch (e: any) {
    error.value = e?.response?.data?.title || 'Ошибка при сохранении';
  } finally {
    saving.value = false;
  }
}
</script>
