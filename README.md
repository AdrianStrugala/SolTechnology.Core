# SolTechnology.Core


I will fill this one day

For SQL DB use:
ISqlConnectionFactory


For Blob:
IBlobConnectionFactory
IBlobContainerClientWrapper (read and write operation)


For HttpClient:
IApiClientFactory




For TaleCode:

1) get matches of player by player id
http://api.football-data.org/v2/players/44/matches?limit=1

2) get match details by match id
https://api.football-data.org/v2/matches/327130

3) store match result

4) calculate how many matches of player led to team victory


{
    "count": 1,
    "filters": {
        "permission": "TIER_ONE",
        "limit": 1
    },
    "player": {
        "id": 44,
        "name": "Cristiano Ronaldo",
        "firstName": "Cristiano Ronaldo",
        "lastName": "dos Santos Aveiro",
        "dateOfBirth": "1985-02-05",
        "countryOfBirth": "Portugal",
        "nationality": "Portugal",
        "position": "Attacker",
        "shirtNumber": null,
        "lastUpdated": "2021-10-13T08:04:10Z"
    },
    "matches": [
        {
            "id": 327130,
            "competition": {
                "id": 2021,
                "name": "Premier League"
            },
            "season": {
                "id": 733,
                "startDate": "2021-08-13",
                "endDate": "2022-05-22",
                "currentMatchday": 25
            },
            "utcDate": "2022-02-08T20:00:00Z",
            "status": "FINISHED",
            "matchday": 24,
            "stage": "REGULAR_SEASON",
            "group": null,
            "lastUpdated": "2022-02-11T16:20:29Z",
            "odds": {
                "msg": "Activate Odds-Package in User-Panel to retrieve odds."
            },
            "score": {
                "winner": "DRAW",
                "duration": "REGULAR",
                "fullTime": {
                    "homeTeam": 1,
                    "awayTeam": 1
                },
                "halfTime": {
                    "homeTeam": 0,
                    "awayTeam": 1
                },
                "extraTime": {
                    "homeTeam": null,
                    "awayTeam": null
                },
                "penalties": {
                    "homeTeam": null,
                    "awayTeam": null
                }
            },
            "homeTeam": {
                "id": 328,
                "name": "Burnley FC"
            },
            "awayTeam": {
                "id": 66,
                "name": "Manchester United FC"
            },
            "referees": [
                {
                    "id": 11495,
                    "name": "Ian Hussin",
                    "role": "ASSISTANT_REFEREE_N1",
                    "nationality": "England"
                },
                {
                    "id": 11480,
                    "name": "Eddie Smart",
                    "role": "ASSISTANT_REFEREE_N2",
                    "nationality": "England"
                },
                {
                    "id": 11378,
                    "name": "Tony Harrington",
                    "role": "FOURTH_OFFICIAL",
                    "nationality": "England"
                },
                {
                    "id": 11575,
                    "name": "Mike Dean",
                    "role": "REFEREE",
                    "nationality": "England"
                },
                {
                    "id": 11494,
                    "name": "Stuart Attwell",
                    "role": "VIDEO_ASSISANT_REFEREE_N1",
                    "nationality": "England"
                },
                {
                    "id": 11431,
                    "name": "Daniel Robathan",
                    "role": "VIDEO_ASSISANT_REFEREE_N2",
                    "nationality": "England"
                }
            ]
        }
    ]
}


{
    "head2head": {
        "numberOfMatches": 10,
        "totalGoals": 26,
        "homeTeam": {
            "id": 328,
            "name": "Burnley FC",
            "wins": 1,
            "draws": 3,
            "losses": 6
        },
        "awayTeam": {
            "id": 66,
            "name": "Manchester United FC",
            "wins": 6,
            "draws": 3,
            "losses": 1
        }
    },
    "match": {
        "id": 327130,
        "competition": {
            "id": 2021,
            "name": "Premier League",
            "area": {
                "name": "England",
                "code": "ENG",
                "ensignUrl": "https://crests.football-data.org/770.svg"
            }
        },
        "season": {
            "id": 733,
            "startDate": "2021-08-13",
            "endDate": "2022-05-22",
            "currentMatchday": 25,
            "winner": null
        },
        "utcDate": "2022-02-08T20:00:00Z",
        "status": "FINISHED",
        "venue": "Turf Moor",
        "matchday": 24,
        "stage": "REGULAR_SEASON",
        "group": null,
        "lastUpdated": "2022-02-11T16:20:29Z",
        "odds": {
            "msg": "Activate Odds-Package in User-Panel to retrieve odds."
        },
        "score": {
            "winner": "DRAW",
            "duration": "REGULAR",
            "fullTime": {
                "homeTeam": 1,
                "awayTeam": 1
            },
            "halfTime": {
                "homeTeam": 0,
                "awayTeam": 1
            },
            "extraTime": {
                "homeTeam": null,
                "awayTeam": null
            },
            "penalties": {
                "homeTeam": null,
                "awayTeam": null
            }
        },
        "homeTeam": {
            "id": 328,
            "name": "Burnley FC"
        },
        "awayTeam": {
            "id": 66,
            "name": "Manchester United FC"
        },
        "referees": [
            {
                "id": 11575,
                "name": "Mike Dean",
                "role": "REFEREE",
                "nationality": "England"
            }
        ]
    }
}