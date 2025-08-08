<template>
    <div :class="memberClass">
        <div class="member-info">
            <div class="username">{{ displayName }}</div>
            <div
                v-if="pronouns"
                class="pronouns"
            >
                {{ pronouns }}
            </div>
        </div>
    </div>
</template>

<script>
export default {
    name: 'VoiceMember',
    props: {
        member: Object,
    },
    computed: {
        memberClass() {
            const classes = ['member'];
            if (this.member.is_speaking) classes.push('speaking');
            return classes.join(' ');
        },
        displayName() {
            // Remove the pronoun part from the display name if it exists
            return this.member.display_name.replace(/\(([^)]+)\)$/, '').trim();
        },
        pronouns() {
            // Extract text within parentheses at the end of the name
            const match = this.member.display_name.match(/\(([^)]+)\)$/);
            return match ? match[1] : null;
        },
    },
};
</script>

<style scoped>
.member {
    background: rgba(255, 255, 255, 0);
    display: flex;
    align-items: center;
    gap: 0.5vw;
    transition: all 0.3s ease;
    border: 1px solid transparent;
    min-width: max-content;
    white-space: nowrap;
    font-family: 'Orbitron', sans-serif;
    color: rgba(255, 255, 255, 75%);
}

.member.speaking {
    border-color: #43b581;
    background: rgba(67, 181, 129, 0.2);
    color: rgba(255, 255, 255, 100%);
    animation: speaking-pulse 1s ease-in-out infinite alternate;
}

@keyframes speaking-pulse {
    from {
        box-shadow: 0 0 0.3vw rgba(67, 181, 129, 0.5);
    }
    to {
        box-shadow: 0 0 0.8vw rgba(67, 181, 129, 0.8);
    }
}

.member-info {
    display: flex;
    flex-direction: column;
    gap: 0;
    min-width: 0;
}

.username {
    font-size: 1.1vw;
    font-weight: 500;
    text-overflow: ellipsis;
    overflow: hidden;
    white-space: nowrap;
}

.pronouns {
    font-size: 0.8vw;
    opacity: 0.7;
    font-weight: 400;
    text-overflow: ellipsis;
    overflow: hidden;
    white-space: nowrap;
    color: rgba(255, 255, 255, 0.8);
}
</style>
