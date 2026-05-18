import {createRouter, createWebHistory, type RouteRecordRaw} from 'vue-router';
import QuestionsList from '@/views/QuestionsList.vue';
import CreateQuestion from '@/views/CreateQuestion.vue';
import ExamGeneration from '@/views/ExamGeneration.vue';

const routes: RouteRecordRaw[] = [
  {path: '/', redirect: '/questions'},
  {path: '/questions', component: QuestionsList},
  {path: '/questions/new', component: CreateQuestion},
  {path: '/exams', component: ExamGeneration},
];

const router = createRouter({
  history: createWebHistory(),
  routes,               // или routes: routes
});

export default router;
