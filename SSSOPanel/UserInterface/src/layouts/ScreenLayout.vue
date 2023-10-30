<template>
  <section class="layout-screen" ref="screen">
    <slot />
  </section>
</template>

<script setup>
import {inject, onMounted, ref} from "vue";
import router from "@/router";

const emitter = inject('emitter');
const screen = ref(null);

const updateScreen = () => {
  let width = window.innerWidth;
  let height = window.innerHeight;
  let aspectRatio = width / height;

  if (aspectRatio >= 16 / 9) {
    screen.value.style.width = `${ (height * 16) / 9 }px`;
    screen.value.style.height = `${ height }px`;
  } else {
    screen.value.style.width = `${ width }px`;
    screen.value.style.height = `${ (width * 9) / 16 }px`;
  }

  screen.value.style.fontSize = `${ width * 0.015 }px`;
};

onMounted(() => {
  updateScreen();
  window.addEventListener('resize', updateScreen);
});

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
  background: #222;
  display: grid;
  overflow: hidden;
  align-self: center;
  justify-self: center;
  font-family: 'Orbitron', sans-serif;
}
</style>