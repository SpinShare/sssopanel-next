<template>
  <AppLayout title="Countdown">
    
    <label>
      <input type="checkbox" v-model="countdownActive" />
      HasCountdown
    </label><br />
    
    <input type="datetime-local" v-model="countdownTime" v-if="countdownActive" /><br />
    
    <button @click="transition">Transition</button>
  </AppLayout>
</template>

<script setup>
import AppLayout from "../../layouts/AppLayout.vue";
import {ref} from "vue";

const countdownActive = ref(false);
const countdownTime = ref(new Date());

const transition = () => {
  window.external.sendMessage(JSON.stringify({
    command: "screen-navigate",
    data: {
      path: "countdown",
      params: {},
      query: countdownActive.value === true ? {countdownTime: countdownTime.value} : {},
    }
  }));
}
</script>

<style lang="scss" scoped>
</style>