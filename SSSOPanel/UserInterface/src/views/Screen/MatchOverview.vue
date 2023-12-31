<template>
    <ScreenLayout>
        <section class="screen-match-overview">Match Overview</section>
    </ScreenLayout>
</template>

<script setup>
import ScreenLayout from '../../layouts/ScreenLayout.vue';
</script>

<style lang="scss" scoped>
.screen-match-overview {
    display: flex;
    justify-content: center;
    align-items: center;

    & img {
        height: 10%;
    }
}
</style>
