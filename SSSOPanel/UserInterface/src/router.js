import * as VueRouter from 'vue-router';

import ViewPanelDashboard from './views/Panel/Dashboard.vue';
import ViewPanelSettings from './views/Panel/Settings.vue';

import ViewPanelInit from './views/Panel/Init.vue';
import ViewPanelCountdown from './views/Panel/Countdown.vue';
import ViewPanelBrackets from './views/Panel/Brackets.vue';
import ViewPanelMatchOverview from './views/Panel/MatchOverview.vue';
import ViewPanelMatchIngame from './views/Panel/MatchIngame.vue';
import ViewPanelCommentators from './views/Panel/Commentators.vue';
import ViewPanelEndStream from './views/Panel/EndStream.vue';
import ViewPanelEndTournament from './views/Panel/EndTournament.vue';

import ViewScreenInit from './views/Screen/Init.vue';
import ViewScreenCountdown from './views/Screen/Countdown.vue';
import ViewScreenBrackets from './views/Screen/Brackets.vue';
import ViewScreenMatchOverview from './views/Screen/MatchOverview.vue';
import ViewScreenMatchIngame from './views/Screen/MatchIngame.vue';
import ViewScreenCommentators from './views/Screen/Commentators.vue';
import ViewScreenEndStream from './views/Screen/EndStream.vue';
import ViewScreenEndTournament from './views/Screen/EndTournament.vue';

import ViewError from './views/Error.vue';

const routes = [
    {
        path: '/panel',
        component: ViewPanelDashboard,
    },
    {
        path: '/panel/settings',
        component: ViewPanelSettings,
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
        path: '/panel/brackets',
        component: ViewPanelBrackets,
    },
    {
        path: '/panel/match-overview',
        component: ViewPanelMatchOverview,
    },
    {
        path: '/panel/match-ingame',
        component: ViewPanelMatchIngame,
    },
    {
        path: '/panel/commentators',
        component: ViewPanelCommentators,
    },
    {
        path: '/panel/end-stream',
        component: ViewPanelEndStream,
    },
    {
        path: '/panel/end-tournament',
        component: ViewPanelEndTournament,
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
        path: '/screen/brackets',
        component: ViewScreenBrackets,
    },
    {
        path: '/screen/match-overview',
        component: ViewScreenMatchOverview,
    },
    {
        path: '/screen/match-ingame',
        component: ViewScreenMatchIngame,
    },
    {
        path: '/screen/commentators',
        component: ViewScreenCommentators,
    },
    {
        path: '/screen/end-stream',
        component: ViewScreenEndStream,
    },
    {
        path: '/screen/end-tournament',
        component: ViewScreenEndTournament,
    },
    {
        path: '/:pathMatch(.*)*',
        component: ViewError,
    },
];

const router = VueRouter.createRouter({
    history: VueRouter.createWebHashHistory(),
    routes,
});

export default router;
