<template>
    <!-- Not an alert, that would replace an open winner prompt. Folds into a header button (AppLayout) so it never
         covers the page for long, requests go on their own after a few minutes -->
    <div class="ref-den-spotlight-prompt">
        <div
            v-if="open"
            class="panel"
        >
            <div class="head">
                <span class="mdi mdi-bullhorn-outline"></span>
                <span class="grow">Ref Den Spotlight</span>
                <button
                    class="icon"
                    title="Fold"
                    @click="open = false"
                >
                    <span class="mdi mdi-chevron-up"></span>
                </button>
            </div>

            <!-- What's up big on stream, it stays until it's hidden here -->
            <div
                v-if="activeSpotlight"
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

            <!-- Only the newest request, the rest wait behind it -->
            <div
                v-if="newestRequest"
                class="request"
            >
                <div class="title">
                    <span class="mdi mdi-bullhorn-outline"></span>
                    <span class="grow">Show on stream?</span>
                    <span
                        v-if="spotlightRequests.length > 1"
                        class="more"
                        >+{{ spotlightRequests.length - 1 }} more</span
                    >
                </div>
                <div class="preview">
                    <span
                        class="author"
                        :style="newestRequest.color ? { color: newestRequest.color } : null"
                        >{{ newestRequest.author }}</span
                    >
                    <span>{{ newestRequest.text }}</span>
                </div>
                <div class="actions">
                    <SpinButton
                        v-if="spotlightRequests.length > 1"
                        label="Dismiss All"
                        @click="dismissAllSpotlights"
                    />
                    <SpinButton
                        label="Dismiss"
                        @click="dismissSpotlight(newestRequest.messageId)"
                    />
                    <SpinButton
                        label="Show"
                        color="primary"
                        @click="showSpotlight(newestRequest.messageId)"
                    />
                </div>
            </div>

            <!-- Any of the last messages, picked by hand -->
            <div class="pick">
                <SpinSelect
                    v-model="picked"
                    :options="messageOptions"
                    :disabled="!messageOptions.length"
                />
                <SpinButton
                    label="Show"
                    :disabled="!picked"
                    @click="showPicked"
                />
            </div>
        </div>
    </div>
</template>

<script setup>
import { computed, ref, watch } from 'vue';
import SpinButton from '@/components/Common/SpinButton.vue';
import SpinSelect from '@/components/Common/SpinSelect.vue';
import { replyText } from '@/modules/refDenMarkup';
import { activeSpotlight, dismissAllSpotlights, dismissSpotlight, hideSpotlight, refDenMessageList, showSpotlight, spotlightOpen, spotlightRequests } from '@/modules/useRefDenRelay';

// Folded into the header button (AppLayout), a new request opens it once
const open = spotlightOpen;
// and folds again once they're answered or expired. Opened by hand it stays open
let openedByRequest = false;
watch(open, (value) => {
    if (!value) openedByRequest = false;
});
watch(
    () => spotlightRequests.value.length,
    (count, before) => {
        if (count > (before ?? 0) && !open.value) {
            open.value = true;
            openedByRequest = true;
        } else if (count === 0 && openedByRequest) {
            open.value = false;
        }
    },
);

const newestRequest = computed(() => spotlightRequests.value[spotlightRequests.value.length - 1] ?? null);

const picked = ref('');
const messageOptions = computed(() =>
    refDenMessageList.value.map((message) => {
        const text = (message.content ?? '').trim() ? replyText(message.content) : '(attachment)';
        return { icon: message.messageId === activeSpotlight.value?.messageId ? 'broadcast' : 'message-text-outline', label: `${message.author}: ${text}`, value: message.messageId };
    }),
);
// A picked message that left the list can't be shown anymore
watch(messageOptions, (options) => {
    if (picked.value && !options.some((option) => option.value === picked.value)) picked.value = '';
});

const showPicked = () => {
    if (!picked.value) return;
    showSpotlight(picked.value);
    picked.value = '';
};
</script>

<style lang="scss" scoped>
.ref-den-spotlight-prompt {
    position: fixed;
    right: 20px;
    // Under the header button it folds into
    top: 56px;
    z-index: 1000;
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    pointer-events: none;

    & > * {
        pointer-events: auto;
    }
}

.panel {
    width: 320px;
    background: rgb(var(--colorBase2));
    color: rgb(var(--colorBaseText));
    border-radius: 6px;
    padding: 10px 12px 12px;
    display: flex;
    flex-direction: column;
    gap: 10px;
    box-shadow: 0 6px 20px rgba(0, 0, 0, 0.4);

    & .head {
        display: flex;
        align-items: center;
        gap: 6px;
        font-weight: 600;
        font-size: 0.85rem;
        opacity: 0.8;
    }

    & .grow {
        flex-grow: 1;
    }

    & .icon {
        border: 0;
        background: none;
        color: inherit;
        cursor: pointer;
        font-size: 20px;
        padding: 0;
        line-height: 1;
    }
}

.request {
    border-left: 3px solid rgb(var(--colorPrimary));
    background: rgba(var(--colorBaseText), 0.04);
    border-radius: 4px;
    padding: 8px 10px;
    display: flex;
    flex-direction: column;
    gap: 6px;

    &.active {
        border-left-color: rgb(var(--colorSuccess));
    }

    & .title {
        display: flex;
        align-items: center;
        gap: 6px;
        font-weight: 600;
        font-size: 0.85rem;
    }

    & .more {
        font-weight: 400;
        opacity: 0.6;
    }

    // Two lines is enough to tell what it is
    & .preview {
        color: rgba(var(--colorBaseText), 0.7);
        font-size: 0.8rem;
        line-height: 1.35;
        word-break: break-word;
        display: -webkit-box;
        -webkit-box-orient: vertical;
        -webkit-line-clamp: 2;
        overflow: hidden;

        & .author {
            font-weight: 700;
            color: rgb(var(--colorBaseText));
            margin-right: 6px;
        }
    }

    & .actions {
        display: flex;
        gap: 6px;
        justify-content: flex-end;
    }
}

.pick {
    display: flex;
    gap: 8px;

    & .spin-select {
        flex-grow: 1;
        max-width: none;
        min-width: 0;
    }
}
</style>
