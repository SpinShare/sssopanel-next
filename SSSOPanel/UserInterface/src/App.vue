<template>
    <router-view v-slot="{ Component, route }">
        <transition name="default" mode="out-in">
            <component :is="Component" :key="route.fullPath" />
        </transition>
    </router-view>
</template>

<script setup>
import {ref, inject} from 'vue';
const emitter = inject('emitter');

window.external.receiveMessage((rawResponse) => {
    const response = JSON.parse(rawResponse);
    emitter.emit(response.Command, response.Data);
});
</script>

<style lang="scss">
#app {
    width: 100%;
    height: 100%;
    display: grid;
    overflow: hidden;

    & > main {
        overflow-y: scroll;
    }
}
</style>
