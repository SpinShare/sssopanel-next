import { ref } from 'vue';

const API_URI = 'https://api.start.gg/gql/alpha';

export default function useTournamentAPI(apiToken) {
    const loading = ref(false);

    const loadTournamentEvents = async (slug) => {
        loading.value = true;

        const query = `
            query TournamentQuery($slug: String) {
                tournament(slug: $slug) {
                    id
                    name
                    events {
                        id
                        name
                    }
                }
            }
        `;

        const variables = {
            slug: slug,
        };

        const response = await fetch(API_URI, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                Authorization: 'Bearer ' + apiToken,
            },
            body: JSON.stringify({
                query,
                variables,
            }),
        });

        const { data } = await response.json();
        if (response.ok) {
            return data?.tournament || null;
        }

        loading.value = false;
    };

    return { loadTournamentEvents, loading };
}
