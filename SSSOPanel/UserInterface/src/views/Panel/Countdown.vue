<template>
  <AppLayout title="Countdown">
    <input type="datetime-local" v-model="countdownTime" />
    <button @click="transition">Transition</button>
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
</script>

<style lang="scss" scoped>
</style>