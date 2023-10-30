<template>
  <AppLayout>
    Dashboard<br />
    <button @click="navigateToIndex">Index</button>
    <button @click="navigateToCountdown">Countdown</button>
  </AppLayout>
</template>

<script setup>
import AppLayout from "../../layouts/AppLayout.vue";

const navigateToIndex = () => {
  window.external.sendMessage(JSON.stringify({
    command: "screen-navigate",
    data: {
      path: "",
      params: {},
      query: {},
    }
  }));
}

const navigateToCountdown = () => {
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