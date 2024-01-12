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

    const loadEventPhases = async (eventId) => {
        loading.value = true;

        const query = `
            query Query($eventId: ID) {
              event(id: $eventId) {
                id
                name
                phases {
                  id
                  name
                  phaseGroups {
                    nodes {
                      id
                      startAt
                      displayIdentifier
                    }
                  }
                }
              }
            }
        `;

        const variables = {
            eventId: eventId,
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
            return data?.event?.phases || null;
        }

        loading.value = false;
    };

    const loadPhaseGroup = async (phaseGroupId) => {
        loading.value = true;

        const query = `
            query Query($phaseGroupId: ID) {
              phaseGroup(id: $phaseGroupId) {
                id
                startAt
                displayIdentifier
                sets(page: 1, perPage: 32, sortType: CALL_ORDER) {
                  nodes {
                    identifier
                    slots {
                      entrant {
                        id
                      }
                      standing {
                        placement
                        stats {
                          score {
                            value
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
        `;

        const variables = {
            phaseGroupId: phaseGroupId,
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
            return data?.phaseGroup || null;
        }

        loading.value = false;
    };

    return { loadTournamentEvents, loadEventEntrants, loadEventPhases, loadPhaseGroup, loading };
}
