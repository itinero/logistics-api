// Itinero - OpenStreetMap (OSM) SDK
// Copyright (C) 2016 Abelshausen Ben
// 
// This file is part of Itinero.
// 
// Itinero is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// Itinero is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Itinero. If not, see <http://www.gnu.org/licenses/>.

namespace Itinero.Logistics.API
{
    /// <summary>
    /// Contains helper extension methods.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Matches two string that contain a given percentage of the same characters.
        /// </summary>
        public static bool LevenshteinMatch(this string s, string t, float percentage)
        {
            if (s == null || t == null)
            {
                return false;
            }
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];
            int match = -1;
            int size = System.Math.Max(n, m);

            if (size == 0)
            { // empty strings cannot be matched.
                return false;
            }

            // Step 1
            if (n == 0)
            {
                match = m;
            }

            if (m == 0)
            {
                match = n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = System.Math.Min(
                        System.Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            match = d[n, m];

            // calculate the percentage.
            return ((float)(size - match) / (float)size) > (percentage / 100.0);
        }
    }
}