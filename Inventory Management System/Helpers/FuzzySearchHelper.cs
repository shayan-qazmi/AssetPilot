// Helpers/FuzzySearchHelper.cs
using System;
using System.Linq;

namespace Inventory_Management_System.Helpers
{
    public static class FuzzySearchHelper
    {
        public static bool IsFuzzyMatch(string? source, string query, int maxDistance = 2)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;
            if (source == null) return false;

            string srcLower = source.ToLowerInvariant();
            string qryLower = query.ToLowerInvariant().Trim();

            if (srcLower.Contains(qryLower)) return true;

            int minDistance = GetLevenshteinDistance(srcLower, qryLower);

            var tokens = srcLower.Split(new[] { ' ', '-', '_', '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                int dist = GetLevenshteinDistance(token, qryLower);
                if (dist < minDistance)
                {
                    minDistance = dist;
                }
            }

            return minDistance <= maxDistance;
        }

        public static int FuzzyScore(string? source, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return 0;
            if (source == null) return -10000;

            string srcLower = source.ToLowerInvariant();
            string qryLower = query.ToLowerInvariant().Trim();

            if (srcLower == qryLower)
            {
                return 10000;
            }

            if (srcLower.StartsWith(qryLower))
            {
                return 9000 - (srcLower.Length - qryLower.Length);
            }

            if (srcLower.Contains(qryLower))
            {
                return 8000 - (srcLower.Length - qryLower.Length);
            }

            int minDistance = GetLevenshteinDistance(srcLower, qryLower);
            var tokens = srcLower.Split(new[] { ' ', '-', '_', '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                int dist = GetLevenshteinDistance(token, qryLower);
                if (dist < minDistance)
                {
                    minDistance = dist;
                }
            }

            if (minDistance <= 2)
            {
                return 5000 - (minDistance * 1000) - (srcLower.Length - qryLower.Length);
            }

            return -10000;
        }

        /// <summary>
        /// Strict scoring for structured identifier fields (equipment codes,
        /// employee codes, QR codes, etc.) where a small edit-distance does NOT
        /// imply relevance — "EQ008" and "EQ001" differ by one character but are
        /// completely different records. This skips the Levenshtein fallback
        /// entirely and only rewards exact, prefix, or substring matches.
        /// </summary>
        public static int FuzzyScoreCode(string? source, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return 0;
            if (source == null) return -10000;

            string srcLower = source.ToLowerInvariant();
            string qryLower = query.ToLowerInvariant().Trim();

            if (srcLower == qryLower) return 10000;
            if (srcLower.StartsWith(qryLower)) return 9000 - (srcLower.Length - qryLower.Length);
            if (srcLower.Contains(qryLower)) return 8000 - (srcLower.Length - qryLower.Length);

            return -10000;
        }

        private static int GetLevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;
            if (string.IsNullOrEmpty(t)) return s.Length;

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }
    }
}