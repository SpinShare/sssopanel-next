import { ref } from 'vue';

const API_URI = 'https://spinsha.re/api/';

export default function useSpinShareApi() {
    const loading = ref(false);

    const loadUser = async (userId) => {
        loading.value = true;

        try {
            const endpoint = API_URI + 'user/' + userId;

            const response = await fetch(endpoint, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                },
            });

            const { data } = await response.json();
            if (response.ok) {
                return data || null;
            }
        } catch (e) {
            return null;
        } finally {
            loading.value = false;
        }
    };

    const loadChart = async (chartId) => {
        loading.value = true;

        try {
            const endpoint = API_URI + 'song/' + chartId;

            const response = await fetch(endpoint, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                },
            });

            const { data } = await response.json();
            if (response.ok) {
                return data || null;
            }
        } catch (e) {
            return null;
        } finally {
            loading.value = false;
        }
    };

    return { loadUser, loadChart, loading };
}
