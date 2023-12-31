<template>
    <ScreenLayout>
        <section class="screen-match-ingame">Match Ingame</section>
    </ScreenLayout>
</template>

<script setup>
import ScreenLayout from '../../layouts/ScreenLayout.vue';
</script>

<style lang="scss" scoped>
.screen-match-ingame {
    display: flex;
    justify-content: center;
    align-items: center;

    & img {
        height: 10%;
    }
}
</style>
