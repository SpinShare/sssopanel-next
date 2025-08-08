<template>
    <div class="voice-status-bar">
      <div class="header">Commentary Team</div>
      <div v-if="!isConnected" class="connection-form">
        <h2>Connect</h2>
        <div class="form-group">
          <label>Server:</label>
          <input
            v-model="serverUrl"
            type="text"
            placeholder="ws://localhost:8765"
            class="form-input"
          />
        </div>
        <div class="form-group">
          <label>Channel ID:</label>
          <input
            v-model="channelId"
            type="text"
            placeholder="Channel ID"
            class="form-input"
          />
        </div>
        <button @click="connectToBot" class="connect-btn">Connect</button>
      </div>
  
      <div v-if="isConnected" class="status">
        <!-- {{ botUsername }} -->
        <!-- <button @click="disconnect" class="disconnect-btn">Disconnect</button> -->
      </div>
  
      <div v-if="isConnected" class="voice-members">
        <div v-if="voiceMembers.length === 0" class="no-members">
          No members in voice channel
        </div>
        <div v-else class="members-container">
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
  import VoiceBotService from "../../modules/VoiceBotService"
  import VoiceMember from "./VoiceMember.vue";
  
  export default {
    name: "ScreenVCHeader",
    components: {
      VoiceMember,
    },
    data() {
      return {
        serverUrl: 'ws://100.104.94.41:8765',
        channelId: "747601413172887552",
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
  * {
    margin: 0;
    padding: 0;
    box-sizing: border-box;
  }
  .header {
  font-size:1.3vw;
  font-weight:600;
  margin-right:3vw;
}
  .voice-status-bar {
    height: 3vw;
    width: 100%;
    background: linear-gradient(270deg, rgba(29, 29, 29, 0.9), rgba(0, 0, 0, 0.9));
    backdrop-filter: blur(10px);
    border-bottom: 2px solid rgba(255, 255, 255, 0.1);
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 1vh 2vw;
    position: relative;
    font-family: 'Orbitron', sans-serif;
    color: #fff;
    overflow: hidden;
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
  
  .connect-btn, .disconnect-btn {
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
  
  .connect-btn:hover, .disconnect-btn:hover {
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
    overflow-x: auto;
    overflow-y: hidden;
    padding: 0 1vw;
    min-width: 0;
    overflow: hidden;
  }
  
  .voice-members::-webkit-scrollbar {
    height: 0.2vw;
  }
  
  .voice-members::-webkit-scrollbar-track {
    background: rgba(255, 255, 255, 0.1);
    border-radius: 0.1vw;
  }
  
  .voice-members::-webkit-scrollbar-thumb {
    background: rgba(255, 255, 255, 0.3);
    border-radius: 0.1vw;
  }
  
  .no-members {
    font-size: 0.9vw;
    color: rgba(255, 255, 255, 0.7);
    white-space: nowrap;
  }
  
  .members-container {
    display: flex;
    gap: 0.8vw;
    align-items: center;
    height: 100%;
    min-width: max-content;
  }
  
  .member {
    background: rgba(255, 255, 255, 0.03);
    border-radius: 0.4vw;
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
  
  .member:hover {
    background: rgba(255, 255, 255, 0.15);
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
  
  .member.muted {
    opacity: 0.6;
  }
  
  .member.deafened {
    opacity: 0.4;
  }
  
  .avatar {
    width: 2.5vw;
    height: 2.5vw;
    border-radius: 50%;
    border: 0.1vw solid rgba(255, 255, 255, 0.3);
    transition: all 0.3s ease;
    flex-shrink: 0;
  }
  
  .speaking .avatar {
    border-color: #43b581;
    animation: avatar-glow 1s ease-in-out infinite alternate;
  }
  
  @keyframes avatar-glow {
    from {
      box-shadow: 0 0 0.2vw rgba(67, 181, 129, 0.5);
    }
    to {
      box-shadow: 0 0 0.6vw rgba(67, 181, 129, 1);
    }
  }
  
  .member-info {
    display: flex;
    flex-direction: column;
    gap: 0vw;
    min-width: 0;
  }
  
  .username {
    font-size: 0.8vw;
    font-weight: 600;
    text-overflow: ellipsis;
    overflow: hidden;
    max-width: 8vw;
    white-space: nowrap;
  }
  
  .status-indicators {
    display: flex;
    gap: 0.2vw;
    align-items: center;
  }
  
  .indicator {
    font-size: 0.6vw;
    padding: 0.1vw 0.3vw;
    border-radius: 0.3vw;
    background: rgba(0, 0, 0, 0.3);
    line-height: 1;
  }
  
  .speaking-indicator {
    background: rgba(67, 181, 129, 0.8);
    animation: indicator-pulse 0.8s ease-in-out infinite alternate;
  }
  
  @keyframes indicator-pulse {
    from {
      transform: scale(1);
    }
    to {
      transform: scale(1.1);
    }
  }
  
  .muted-indicator {
    background: rgba(240, 71, 71, 0.8);
  }
  
  .deafened-indicator {
    background: rgba(153, 170, 181, 0.8);
  }
  
  /* Very small screens */
  @media (max-width: 1024px) {
    .form-input {
      width: 10vw;
    }
    
    .username {
      max-width: 6vw;
    }
  }
  </style>