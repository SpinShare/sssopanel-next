<template>
  <section class="layout-app">
    <header>
      <SpinButton
          v-if="router.currentRoute.value.fullPath !== '/panel'"
          icon="arrow-left"
          color="transparent"
          @click="router.push({ path: '/panel'})"
      />
      <div class="title">
        {{ props.title }}
      </div>
      
      <nav>
        <SpinButton
            icon="open-in-app"
            color="transparent"
            @click="openScreen"
        />
      </nav>
    </header>
    <main>
      <slot />
    </main>
    <footer>
      Current: {{ currentScreen }}
    </footer>

    <UpdateBanner />
    <AlertMessage />
  </section>
</template>

<script setup>
import UpdateBanner from "@/components/UpdateBanner.vue";
import AlertMessage from "@/components/Common/AlertMessage.vue";
import {inject, onMounted, ref} from "vue";
import router from "@/router";

const emitter = inject('emitter');

const props = defineProps({
  title: {
    type: String,
    default: "SpinShare",
  },
});

const currentScreen = ref("");

const openScreen = () => {
  window.external.sendMessage(JSON.stringify({
    command: "open-screen",
  }));
};

onMounted(() => {
  window.external.sendMessage(JSON.stringify({
    command: "current-route-get",
  }));
});

emitter.on('screen-navigate-response', (data) => {
  currentScreen.value = data.Path;
});

emitter.on('current-route-get-response', (data) => {
  currentScreen.value = data.Path;
});
</script>

<style lang="scss" scoped>
.layout-app {
  width: 100%;
  height: 100%;
  display: grid;
  overflow: hidden;
  grid-template-rows: 50px 1fr;
  
  & header {
    display: flex;
    gap: 15px;
    align-items: center;
    padding: 0 20px;
    border-bottom: 1px solid rgba(255,255,255,0.1);
    
    & .title {
      flex-grow: 1;
    }
    
    & nav {
      display: flex;
      gap: 10px;
    }
  }

  & > main {
    overflow-y: auto;
  }
}
</style>
