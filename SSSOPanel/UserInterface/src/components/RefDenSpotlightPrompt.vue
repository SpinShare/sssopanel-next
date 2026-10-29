<template>
    <!-- Not an alert, that would replace an open winner prompt. Stays until it's answered -->
    <TransitionGroup
        tag="div"
        name="request"
        class="ref-den-spotlight-prompt"
    >
        <!-- What's up big on stream, it stays until it's hidden here -->
        <div
            v-if="activeSpotlight"
            key="active"
            class="request active"
        >
            <div class="title">
                <span class="mdi mdi-broadcast"></span>
                <span>On stream now</span>
            </div>
            <div class="preview">
                <span
                    class="author"
                    :style="activeSpotlight.color ? { color: activeSpotlight.color } : null"
                    >{{ activeSpotlight.author }}</span
                >
                <span>{{ activeSpotlight.text }}</span>
            </div>
            <div class="actions">
                <SpinButton
                    label="Hide"
                    color="primary"
                    @click="hideSpotlight"
                />
            </div>
        </div>
        <div
            v-for="request in spotlightRequests"
            :key="request.messageId"
            class="request"
        >
            <div class="title">
                <span class="mdi mdi-bullhorn-outline"></span>
                <span>Show on stream?</span>
            </div>
            <div class="preview">
                <span
                    class="author"
                    :style="request.color ? { color: request.color } : null"
                    >{{ request.author }}</span
                >
                <span>{{ request.text }}</span>
            </div>
            <div class="actions">
                <SpinButton
                    label="Dismiss"
                    @click="dismissSpotlight(request.messageId)"
                />
                <SpinButton
                    label="Show"
                    color="primary"
                    @click="showSpotlight(request.messageId)"
                />
            </div>
        </div>
    </TransitionGroup>
</template>

<script setup>
import SpinButton from '@/components/Common/SpinButton.vue';
import { activeSpotlight, dismissSpotlight, hideSpotlight, showSpotlight, spotlightRequests } from '@/modules/useRefDenRelay';
</script>

<style lang="scss" scoped>
.ref-den-spotlight-prompt {
    position: fixed;
    right: 20px;
    // Above the page footer, its transition buttons stay reachable
    bottom: 90px;
    z-index: 1000;
    width: 340px;
    display: flex;
    flex-direction: column;
    gap: 10px;
    pointer-events: none;
}

.request {
    pointer-events: auto;
    background: rgb(var(--colorBase2));
    color: rgb(var(--colorBaseText));
    border-left: 3px solid rgb(var(--colorPrimary));
    border-radius: 6px;
    padding: 12px 14px;
    display: flex;
    flex-direction: column;
    gap: 8px;
    box-shadow: 0 6px 20px rgba(0, 0, 0, 0.4);

    &.active {
        border-left-color: rgb(var(--colorSuccess));
    }

    & .title {
        display: flex;
        align-items: center;
        gap: 6px;
        font-weight: 600;
        font-size: 0.9rem;
    }

    // A few lines is enough to tell what it is
    & .preview {
        color: rgba(var(--colorBaseText), 0.7);
        font-size: 0.85rem;
        line-height: 1.35;
        word-break: break-word;
        display: -webkit-box;
        -webkit-box-orient: vertical;
        -webkit-line-clamp: 3;
        overflow: hidden;

        & .author {
            font-weight: 700;
            color: rgb(var(--colorBaseText));
            margin-right: 6px;
        }
    }

    & .actions {
        display: flex;
        gap: 8px;
        justify-content: flex-end;
    }
}

.request-enter-active,
.request-leave-active {
    transition: opacity 0.3s ease, transform 0.3s ease;
}
.request-enter-from,
.request-leave-to {
    opacity: 0;
    transform: translateX(20px);
}
.request-move {
    transition: transform 0.3s ease;
}
</style>
