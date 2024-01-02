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

    const loadEventEntrants = async (eventId) => {
        loading.value = true;

        const query = `
            query EventEntrants($eventId: ID!, $page: Int!, $perPage: Int!) {
              event(id: $eventId) {
                id
                name
                entrants(query: {
                  page: $page
                  perPage: $perPage
                }) {
                  pageInfo {
                    total
                    totalPages
                  }
                  nodes {
                    id
                    name
                    isDisqualified
                  }
                }
              }
            }
        `;

        const variables = {
            eventId: eventId,
            page: 1,
            perPage: 100,
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
            return data?.event?.entrants?.nodes || null;
        }

        loading.value = false;
    };

    return { loadTournamentEvents, loadEventEntrants, loading };
}
