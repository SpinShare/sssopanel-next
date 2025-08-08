<template>
    <div :class="memberClass">
        <div class="member-info">
            <div class="username">{{ member.display_name }}</div>
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
    },
};
</script>

<style scoped>
.member {
    background: rgba(255, 255, 255, 0);
    border-radius: 0.4vw;
    padding: 0.3vw 0.6vw;
    margin-right: 2vw;
    display: flex;
    align-items: center;
    gap: 0.5vw;
    transition: all 0.3s ease;
    border: 1px solid transparent;
    min-width: max-content;
    white-space: nowrap;
    font-family: 'Orbitron', sans-serif;
}

.member.speaking {
    border-color: #43b581;
    background: rgba(67, 181, 129, 0.2);
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
    max-width: 10vw;
    white-space: nowrap;
}
</style>
