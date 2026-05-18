<script setup lang="ts">
import {
  BApp,
  BNavbar,
  BNavbarBrand,
  BNavbarNav,
  BNavItem,
  BLink
} from 'bootstrap-vue-next'
</script>

<template>
  <BApp>
    <header>
      <BNavbar toggleable="lg" variant="light" bg="light">
        <BNavbarBrand to="/">Генератор экзаменов</BNavbarBrand>
        <BNavbarNav>
          <BNavItem>
            <BLink to="/questions" exact-active-class="active">Вопросы</BLink>
          </BNavItem>
          <BNavItem>
            <BLink to="/exams" exact-active-class="active">Генерация билетов</BLink>
          </BNavItem>
        </BNavbarNav>
      </BNavbar>
    </header>

    <main>
      <router-view/>
    </main>
  </BApp>
</template>

<style>
/* Дополнительные глобальные стили при необходимости */
</style>
