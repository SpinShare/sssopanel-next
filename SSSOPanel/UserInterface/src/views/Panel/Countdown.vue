<template>
  <AppLayout title="Countdown">
    <input type="datetime-local" v-model="countdownTime" />
    <button @click="transition">Transition</button>
    <button @click="transitionNoCountdown">Transition no countdown</button>
  </AppLayout>
</template>

<script setup>
import AppLayout from "../../layouts/AppLayout.vue";
import {ref} from "vue";

const countdownTime = ref(new Date());

const transition = () => {
  window.external.sendMessage(JSON.stringify({
    command: "screen-navigate",
    data: {
      path: "countdown",
      params: {},
      query: {
        countdownTime: countdownTime.value,
      },
    }
  }));
}
const transitionNoCountdown = () => {
  window.external.sendMessage(JSON.stringify({
    command: "screen-navigate",
    data: {
      path: "countdown",
      params: {},
      query: {},
    }
  }));
}
</script>

<style lang="scss" scoped>
</style>