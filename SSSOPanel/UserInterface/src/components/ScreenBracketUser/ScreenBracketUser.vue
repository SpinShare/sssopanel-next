<template>
    <div
        class="player"
        :class="{ winner: player?.placement === 1 }"
    >
        <div>
            <span
                class="pronouns"
                v-if="player?.entrant?.spinshareProfile?.pronouns"
                >({{ player?.entrant?.spinshareProfile?.pronouns }})</span
            >
            <span class="username">{{ player?.entrant?.spinshareProfile?.username }}</span>
        </div>
        <span class="score">{{ player?.score }}</span>
    </div>
</template>

<script setup>
defineProps({
    player: {
        type: [Object, Boolean],
        default: false,
    },
});
</script>

<style lang="scss" scoped>
.player {
    display: grid;
    grid-template-columns: 1fr auto;
    gap: 0.3vw 0.5vw;
    padding: 0.5vw 0.5vw;
    align-items: center;

    & > div {
        display: flex;
        flex-direction: column;
        gap: 0.2vw;

        & span.pronouns {
            font-weight: bold;
            font-size: 0.8vw;
            letter-spacing: 0;
        }
        & span.username {
            flex-grow: 1;
            font-size: 1vw;
            font-weight: bold;
            letter-spacing: 0.05vw;
        }
    }
    & span.score {
        background: #222;
        color: #ededed;
        padding: 0.25vw;
        border-radius: 0.25vw;
        font-weight: bold;
    }
    &.winner {
        background: #7fe794;
        border-radius: 0.25vw;
    }
}
</style>
