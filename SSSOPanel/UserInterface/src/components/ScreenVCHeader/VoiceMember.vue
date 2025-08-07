<template>
    <div :class="memberClass">
      <!-- <img :src="member.avatar" :alt="member.username" class="avatar" /> -->
      <div class="member-info">
        <div class="username">{{ member.display_name }}</div>
        <div class="status-indicators">
          <!-- <span
            v-if="member.is_speaking"
            class="indicator speaking-indicator"
          >🎙️</span> -->
          <span v-if="member.is_muted" class="indicator muted-indicator">🔇</span>
          <span v-if="member.is_deafened" class="indicator deafened-indicator">🔇</span>
        </div>
      </div>
    </div>
  </template>
  
  <script>
  export default {
    name: "VoiceMember",
    props: {
      member: Object,
    },
    computed: {
      memberClass() {
        const classes = ["member"];
        if (this.member.is_speaking) classes.push("speaking");
        if (this.member.is_muted) classes.push("muted");
        if (this.member.is_deafened) classes.push("deafened");
        return classes.join(" ");
      },
    },
  };
  </script>

<style scoped>
.member {
  background: rgba(255, 255, 255, 0.0);
  border-radius: 0.4vw;
  padding: 0.3vw 0.6vw;
  margin-right:2vw;
  display: flex;
  align-items: center;
  gap: 0.5vw;
  transition: all 0.3s ease;
  border: 1px solid transparent;
  height: 3.5vw;
  min-width: max-content;
  white-space: nowrap;
  font-family: 'Orbitron', sans-serif;
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
    box-shadow: 0.3vw rgba(67, 181, 129, 0.5);
  }
  to {
    box-shadow: 0.8vw rgba(67, 181, 129, 0.8);
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
    box-shadow: 0.2vw rgba(67, 181, 129, 0.5);
  }
  to {
    box-shadow: 0.6vw rgba(67, 181, 129, 1);
  }
}

.member-info {
  display: flex;
  flex-direction: column;
  gap: 0.0vw;
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
  display: inline-block;
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

/* Optional: support very small screens */
@media (max-width: 1024px) {
  .username {
    max-width: 6vw;
  }
}
</style>