import * as VueRouter from 'vue-router';

import ViewPanelDashboard from './views/Panel/Dashboard.vue';

import ViewPanelInit from './views/Panel/Init.vue';
import ViewPanelCountdown from './views/Panel/Countdown.vue';
import ViewScreenInit from './views/Screen/Init.vue';
import ViewScreenCountdown from './views/Screen/Countdown.vue';

import ViewError from './views/Error.vue';

const routes = [
    {
        path: '/panel',
        component: ViewPanelDashboard,
    },
    {
        path: '/panel/init',
        component: ViewPanelInit,
    },
    {
        path: '/panel/countdown',
        component: ViewPanelCountdown,
    },
    {
        path: '/screen',
        component: ViewScreenInit,
    },
    {
        path: '/screen/countdown',
        component: ViewScreenCountdown,
    },
    {
        path: "/:pathMatch(.*)*",
        component: ViewError
    },
];

const router = VueRouter.createRouter({
    history: VueRouter.createWebHashHistory(),
    routes,
});

export default router;