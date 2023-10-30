import * as VueRouter from 'vue-router';

import ViewPanelDashboard from './views/Panel/Dashboard.vue';
import ViewScreenIndex from './views/Screen/Index.vue';
import ViewScreenCountdown from './views/Screen/Countdown.vue';
import ViewError from './views/Error.vue';

const routes = [
    {
        path: '/panel',
        component: ViewPanelDashboard,
    },
    {
        path: '/screen',
        component: ViewScreenIndex,
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