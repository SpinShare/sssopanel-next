<template>
  <ScreenLayout>
    <section class="screen-streamstart-countdown">
      <div class="noise"></div>
      <div class="countdown">
        <div class="header">STARTING UP...</div>
        <div class="time-left">{{ timeLeft }}</div>
      </div>
      
      <div class="dots left"></div>
      <div class="dots right"></div>
      
      <div class="marquee top">
        <template v-for="n in 10" :key="n">
          <span>SPINSHARE SPEEN OPEN</span>
        </template>
      </div>
      <div class="marquee bottom">
        <template v-for="n in 10" :key="n">
          <span>SPINSHARE SPEEN OPEN</span>
        </template>
      </div>
    </section>
  </ScreenLayout>
</template>

<script setup>
import { useRoute } from 'vue-router';
import ScreenLayout from "../../layouts/ScreenLayout.vue";
import {ref} from "vue";

const route = useRoute();
const countdownDate = new Date(route.query.countdownTime);
const timeLeft = ref('00:00.0');

const pad = (number, length = 2) => {
  return number.toString().padStart(length, '0');
}

setInterval(() => {
  let currentDate = new Date();
  let timeDifference = countdownDate.getTime() - currentDate.getTime();
  
  if(timeDifference > 0) {
    let minutes = Math.floor((timeDifference / (1000 * 60)) % 60);
    let seconds = Math.floor((timeDifference / 1000) % 60);
    let milliseconds = Math.floor((timeDifference % 1000) / 100);

    timeLeft.value = `${pad(minutes)}:${pad(seconds)}.${milliseconds}`;
  } else {
    timeLeft.value = '00:00.0';
  }
}, 50);
</script>

<style lang="scss" scoped>
.screen-streamstart-countdown {
  background: url("../../assets/background.svg");
  background-size: contain;
  color: #222;
  position: relative;
  display: flex;
  justify-content: center;
  align-items: center;
  
  & .noise {
    background: url("../../assets/noise.png");
    background-size: 5em;
    position: absolute;
    left: 0;
    right: 0;
    bottom: 0;
    top: 0;
    mix-blend-mode: soft-light;
    animation: noise 2s steps(2) infinite;
  }
  
  & .countdown {
    background: #111111;
    color: #dcdcdc;
    padding: 0.5em 2.5em;
    border-radius: 0 0.5em 0.5em 0.5em;
    position: relative;
    
    & .header {
      font-size: 1em;
      font-weight: 900;
      letter-spacing: 0.15em;
      background: #111111;
      position: absolute;
      top: -1.45em;
      left: 0;
      padding: 0.5em 1em;
      border-top-left-radius: 0.5em;
      border-top-right-radius: 0.5em;
    }
    
    & .time-left {
      font-family: 'JetBrains Mono', monospace;
      font-weight: 900;
      font-size: 5em;
    }
  }
  
  & .dots {
    position: absolute;
    top: 3em;
    width: 3em;
    bottom: 3em;
    background-size: 1em 1em;
    background-image: radial-gradient(circle, #000 10%, transparent 12%), radial-gradient(circle, #000 10%, transparent 12%);
    
    &.left {
      left: 1em;
    }
    &.right {
      right: 1em;
    }
  }
  
  & .marquee {
    white-space: nowrap;
    display: flex;
    gap: 1em;
    position: absolute;
    overflow: hidden;
    font-size: 1em;
    font-weight: 600;
    
    &.top {
      top: 1.25em;
      left: -15.26em;
      animation: marqueeLeft 10s infinite linear;
    }
    &.bottom {
      bottom: 1.25em;
      left: 0;
      animation: marqueeRight 10s infinite linear;
    }
  }
}

@keyframes noise {
  0% { background-position: 0 0; }
  10% { background-position: -1rem -4rem; }
  20% { background-position: -8rem 2rem; }
  30% { background-position: 9rem -9rem; }
  40% { background-position: -2rem 7rem; }
  50% { background-position: -9rem -4rem; }
  60% { background-position: 2rem 6rem; }
  70% { background-position: 7rem -8rem; }
  80% { background-position: -9rem 1rem; }
  90% { background-position: 6rem -5rem; }
  100% { background-position: -7rem 0rem; }
}

@keyframes marqueeLeft {
  from {
    left: -15.26em;
  }
  to {
    left: 0;
  }
}

@keyframes marqueeRight {
  from {
    left: 0;
  }
  to {
    left: -15.26em;
  }
}
</style>