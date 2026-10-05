using System;
using System.Collections.Generic;

namespace ASTeams.SingleLine.Core
{
    /// <summary>
    /// Groups the destinations into passport pages: each run of consecutive destinations in
    /// the same country is one page. Built once from the region of every destination in
    /// flying order, then answered by lookups only.
    /// </summary>
    public sealed class RouteRegions
    {
        private readonly int[] firstDestination;
        private readonly int[] pageOfDestination;

        public RouteRegions(IReadOnlyList<string> regionOfDestination)
        {
            if (regionOfDestination == null)
            {
                throw new ArgumentNullException(nameof(regionOfDestination));
            }

            var starts = new List<int>();
            pageOfDestination = new int[regionOfDestination.Count];

            for (int i = 0; i < regionOfDestination.Count; i++)
            {
                if (i == 0 || regionOfDestination[i] != regionOfDestination[i - 1])
                {
                    starts.Add(i);
                }

                pageOfDestination[i] = starts.Count - 1;
            }

            starts.Add(regionOfDestination.Count);
            firstDestination = starts.ToArray();

            for (int page = 0; page < PageCount; page++)
            {
                LargestPage = Math.Max(LargestPage, DestinationCount(page));
            }
        }

        public int PageCount => firstDestination.Length - 1;

        /// <summary>Most destinations on any one page: how many markers a page view must hold.</summary>
        public int LargestPage { get; }

        public int FirstDestination(int page)
        {
            return firstDestination[page];
        }

        public int DestinationCount(int page)
        {
            return firstDestination[page + 1] - firstDestination[page];
        }

        /// <summary>The page a destination is on; a destination past the end stays on the last page.</summary>
        public int PageOf(int destination)
        {
            if (pageOfDestination.Length == 0)
            {
                return 0;
            }

            return pageOfDestination[Math.Max(0, Math.Min(destination, pageOfDestination.Length - 1))];
        }
    }
}
