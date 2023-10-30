<template>
  <section class="layout-screen">
    <slot />
  </section>
</template>

<script setup>
import {inject, onMounted} from "vue";
import router from "@/router";

const emitter = inject('emitter');

emitter.on('screen-navigate-response', (data) => {
  router.push({
    path: data.Path,
    query: data.Query,
    params: data.Params,
  });
});

emitter.on('current-route-get-response', (data) => {
  router.push({
    path: data.Path,
    query: data.Query,
    params: data.Params,
  });
});

onMounted(() => {
  window.external.sendMessage(JSON.stringify({
    command: "current-route-get",
  }));
});
</script>

<style lang="scss">
.layout-screen {
  width: 100%;
  height: 100%;
  overflow: hidden;
}
</style>