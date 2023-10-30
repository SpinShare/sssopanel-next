<template>
  <AppLayout title="Dashboard">
    <button @click="router.push({ path: '/panel/init'})">Init</button><br />
    <button @click="router.push({ path: '/panel/countdown'})">Countdown</button>
  </AppLayout>
</template>

<script setup>
import AppLayout from "../../layouts/AppLayout.vue";
import router from "@/router";
</script>

<style lang="scss" scoped>
</style>