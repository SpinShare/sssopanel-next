<template>
  <ScreenLayout>
    <section class="screen-init">
      <img :src="Logo" class="logo" />
    </section>
  </ScreenLayout>
</template>

<script setup>
import ScreenLayout from "../../layouts/ScreenLayout.vue";
import Logo from "../../assets/logo.svg";
</script>

<style lang="scss" scoped>
.screen-init {
  display: flex;
  justify-content: center;
  align-items: center;
  
  & img {
    height: 10%;
  }
}
</style>