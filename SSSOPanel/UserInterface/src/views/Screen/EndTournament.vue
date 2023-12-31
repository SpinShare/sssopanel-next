<template>
    <ScreenLayout>
        <section class="screen-end-tournament">End of Tournament</section>
    </ScreenLayout>
</template>

<script setup>
import ScreenLayout from '../../layouts/ScreenLayout.vue';
</script>

<style lang="scss" scoped>
.screen-end-tournament {
    display: flex;
    justify-content: center;
    align-items: center;

    & img {
        height: 10%;
    }
}
</style>
