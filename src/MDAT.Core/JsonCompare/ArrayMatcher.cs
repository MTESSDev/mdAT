namespace MDAT.JsonCompare;

/// <summary>
/// Pairs the items of two arrays that are equal. The longest common run of items is kept first, then both
/// remaining sides (before and after the run) are matched independently, the same strategy as difflib.
/// </summary>
internal static class ArrayMatcher
{
    /// <summary>Returns, for each item of <paramref name="left"/>, the index of its twin in <paramref name="right"/> or -1.</summary>
    public static int[] MatchInOrder(IReadOnlyList<JsonTree> left, IReadOnlyList<JsonTree> right, Func<JsonTree, JsonTree, bool> areEqual)
    {
        var matches = new int[left.Count];
        Array.Fill(matches, -1);

        Match(left, right, areEqual, 0, left.Count, 0, right.Count, matches);

        return matches;
    }

    private static void Match(IReadOnlyList<JsonTree> left, IReadOnlyList<JsonTree> right, Func<JsonTree, JsonTree, bool> areEqual,
                              int leftStart, int leftEnd, int rightStart, int rightEnd, int[] matches)
    {
        if (leftStart >= leftEnd || rightStart >= rightEnd)
        {
            return;
        }

        var bestLength = 0;
        var bestLeftEnd = -1;
        var bestRightEnd = -1;

        // previous[c] / current[c] = length of the common run ending at the previous / current left item and right item c - 1
        var previous = new int[rightEnd - rightStart + 1];
        var current = new int[rightEnd - rightStart + 1];

        for (var i = leftStart; i < leftEnd; i++)
        {
            for (var j = rightStart; j < rightEnd; j++)
            {
                var column = j - rightStart + 1;
                current[column] = areEqual(left[i], right[j]) ? previous[column - 1] + 1 : 0;

                if (current[column] > bestLength)
                {
                    bestLength = current[column];
                    bestLeftEnd = i;
                    bestRightEnd = j;
                }
            }

            (previous, current) = (current, previous);
            Array.Clear(current);
        }

        if (bestLength == 0)
        {
            return;
        }

        var leftRunStart = bestLeftEnd - bestLength + 1;
        var rightRunStart = bestRightEnd - bestLength + 1;

        for (var k = 0; k < bestLength; k++)
        {
            matches[leftRunStart + k] = rightRunStart + k;
        }

        Match(left, right, areEqual, leftStart, leftRunStart, rightStart, rightRunStart, matches);
        Match(left, right, areEqual, bestLeftEnd + 1, leftEnd, bestRightEnd + 1, rightEnd, matches);
    }
}
