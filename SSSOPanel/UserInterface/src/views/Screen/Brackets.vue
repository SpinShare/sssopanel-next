<template>
    <ScreenLayout>
        <section class="screen-brackets">Brackets</section>
    </ScreenLayout>
</template>

<script setup>
import ScreenLayout from '../../layouts/ScreenLayout.vue';
</script>

<style lang="scss" scoped>
.screen-brackets {
    display: flex;
    justify-content: center;
    align-items: center;

    & img {
        height: 10%;
    }
}
</style>
