<template>
    <div class="voice-status-bar">
        <div 
            v-if="isConnected"
            class="header">
            Commentary Team
        </div>
        <!-- Move all connect and disconnect logic to panel!!! -->
        <div
            v-if="!isConnected"
            class="no-members"
        >
            Bot is not connected. Reconnect in progress...
        </div>

        <div
            v-if="isConnected"
            class="status"
        >
            <!-- {{ botUsername }} -->
            <!-- <button @click="disconnect" class="disconnect-btn">Disconnect</button> -->
        </div>

        <div
            v-if="isConnected"
            class="voice-members"
        >
            <div
                v-if="voiceMembers.length === 0"
                class="no-members"
            >
                No members in voice channel
            </div>
            <div
                v-else
                class="members-container"
            >
                <VoiceMember
                    v-for="member in voiceMembers"
                    :key="member.id"
                    :member="member"
                />
            </div>
        </div>
    </div>
</template>

<script>
import VoiceBotService from '../../modules/VoiceBotService';
import VoiceMember from './VoiceMember.vue';

export default {
    name: 'ScreenVCHeader',
    components: {
        VoiceMember,
    },
    data() {
        return {
            serverUrl: 'ws://207.180.220.125:8765',
            channelId: '747601413172887552',
        };
    },
    computed: {
        state() {
            return VoiceBotService.state;
        },
        isConnected() {
            return this.state.isConnected;
        },
        botUsername() {
            return this.state.botUsername;
        },
        voiceMembers() {
            return this.state.voiceMembers;
        },
    },
    mounted() {
        VoiceBotService.connect(this.serverUrl, this.channelId);
    },
    methods: {
        connectToBot() {
            VoiceBotService.connect(this.serverUrl, this.channelId);
        },
        disconnect() {
            VoiceBotService.disconnect();
        },
    },
};
</script>

<style scoped>
.header {
    font-size: 1.3vw;
    font-weight: 600;
    padding-left: 2vw;
}
.voice-status-bar {
    height: 3vw;
    width: 100%;
    background: #000;
    display: flex;
    align-items: center;
    justify-content: space-between;
    position: relative;
    font-family: 'Orbitron', sans-serif;
    flex-wrap: nowrap;
}

.connection-form {
    display: flex;
    align-items: center;
    gap: 1vw;
    flex: 1;
    justify-content: center;
    min-width: 0;
    overflow: hidden;
}

.connection-form h2 {
    font-size: 1.2vw;
    margin-right: 1vw;
    white-space: nowrap;
}

.form-group {
    display: flex;
    align-items: center;
    gap: 0.5vw;
}

.form-group label {
    font-size: 0.9vw;
    font-weight: 600;
    white-space: nowrap;
}

.form-input {
    padding: 0.3vw 0.8vw;
    border: 1px solid rgba(255, 255, 255, 0.3);
    border-radius: 0.3vw;
    background: rgba(255, 255, 255, 0.1);
    color: white;
    font-size: 0.8vw;
    width: 12vw;
    transition: all 0.3s ease;
}

.form-input:focus {
    outline: none;
    border-color: #43b581;
    background: rgba(255, 255, 255, 0.15);
}

.form-input::placeholder {
    color: rgba(255, 255, 255, 0.6);
    font-size: 0.7vw;
}

.connect-btn,
.disconnect-btn {
    background: linear-gradient(45deg, #43b581, #4caf50);
    color: white;
    border: none;
    padding: 0.4vw 0.8vw;
    border-radius: 0.3vw;
    font-size: 0.8vw;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.3s ease;
    white-space: nowrap;
}

.disconnect-btn {
    background: linear-gradient(45deg, #f04747, #d63939);
}

.connect-btn:hover,
.disconnect-btn:hover {
    transform: scale(1.05);
    filter: brightness(1.1);
}

.status {
    display: flex;
    align-items: center;
    gap: 1vw;
    font-size: 0.9vw;
    font-weight: 600;
    flex-shrink: 0;
    padding-right: 1vw;
    white-space: nowrap;
}

.voice-members {
    flex: 1;
    display: flex;
    align-items: center;
    height: 100%;
    padding: 0 1vw;
    min-width: 0;
    overflow: hidden;
}

.no-members {
    font-size: 0.9vw;
    color: rgba(255, 255, 255, 0.7);
    white-space: nowrap;
    margin-left:2vh;
}

.members-container {
    display: flex;
    gap: 0.8vw;
    align-items: center;
    height: 100%;
    min-width: max-content;
}

.member {
    background: rgba(255, 255, 255, 0);
    padding: 0.3vw 0.6vw;
    display: flex;
    align-items: center;
    gap: 0.5vw;
    transition: all 0.3s ease;
    border: 1px solid transparent;
    height: 3.5vw;
    min-width: max-content;
    white-space: nowrap;
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
</style>
