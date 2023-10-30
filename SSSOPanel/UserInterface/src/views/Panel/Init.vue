<template>
  <AppLayout title="Init">
    <button @click="transition">Transition</button>
  </AppLayout>
</template>

<script setup>
import AppLayout from "../../layouts/AppLayout.vue";

const transition = () => {
  window.external.sendMessage(JSON.stringify({
    command: "screen-navigate",
    data: {
      path: "",
      params: {},
      query: {},
    }
  }));
}
</script>

<style lang="scss" scoped>
</style>