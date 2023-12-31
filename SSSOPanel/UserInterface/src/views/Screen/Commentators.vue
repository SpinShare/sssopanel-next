<template>
    <ScreenLayout>
        <section class="screen-match-commentators">Commentators</section>
    </ScreenLayout>
</template>

<script setup>
import ScreenLayout from '../../layouts/ScreenLayout.vue';
</script>

<style lang="scss" scoped>
.screen-commentators {
    display: flex;
    justify-content: center;
    align-items: center;

    & img {
        height: 10%;
    }
}
</style>
