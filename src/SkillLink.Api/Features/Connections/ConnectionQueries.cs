namespace SkillLink.Api.Features.Connections
{
    internal static class ConnectionQueries
    {
        public const string SuggestionsSql = """
                WITH my_links AS (      -- everyone I have ANY connection row with (~200 rows)
                    SELECT addressee_id AS other_id, status FROM connections WHERE requester_id = @me
                    UNION ALL
                    SELECT requester_id, status FROM connections WHERE addressee_id = @me
                ),
                my_friends AS (
                    SELECT other_id AS friend_id FROM my_links WHERE status = 'Accepted'
                ),
                second_hop AS (
                    SELECT f.friend_id, c.addressee_id AS candidate_id
                    FROM my_friends f JOIN connections c ON c.requester_id = f.friend_id AND c.status = 'Accepted'
                    UNION ALL
                    SELECT f.friend_id, c.requester_id
                    FROM my_friends f JOIN connections c ON c.addressee_id = f.friend_id AND c.status = 'Accepted'
                ),
                ranked AS (             -- aggregate and limit BEFORE touching users
                    SELECT h.candidate_id, COUNT(DISTINCT h.friend_id)::int AS mutual_count
                    FROM second_hop h
                    WHERE h.candidate_id <> @me
                      AND NOT EXISTS (SELECT 1 FROM my_links l WHERE l.other_id = h.candidate_id)
                    GROUP BY h.candidate_id
                    ORDER BY mutual_count DESC
                    LIMIT 10
                )
                SELECT u.id AS "Id", u.display_name AS "DisplayName", r.mutual_count AS "MutualCount"
                FROM ranked r
                JOIN users u ON u.id = r.candidate_id
                ORDER BY r.mutual_count DESC;
                """;
    }
}
