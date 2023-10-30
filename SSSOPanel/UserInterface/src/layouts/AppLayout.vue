<template>
  <section class="layout-app">
    <header>
      <div class="brand">
        SpinShare
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
  </section>

  <UpdateBanner />
  <AlertMessage />
</template>

<script setup>
import UpdateBanner from "@/components/UpdateBanner.vue";
import AlertMessage from "@/components/Common/AlertMessage.vue";

const openScreen = () => {
  window.external.sendMessage(JSON.stringify({
    command: "open-screen",
  }));
};
</script>

<style lang="scss" scoped>
.layout-app {
  width: 100%;
  height: 100%;
  display: grid;
  overflow: hidden;
  grid-template-rows: 50px 1fr;
  
  & header {
    display: grid;
    grid-template-columns: 1fr auto;
    gap: 25px;
    align-items: center;
    padding: 0 20px;
  }

  & > main {
    overflow-y: auto;
  }
}
</style>
