using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ImageViewer.Utils;

public class NaturalComparer : IComparer<string>
{
    private static int CompareChunks(string x, string y) {
        if (x[0] >= '0' && x[0] <= '9' && y[0] >= '0' && y[0] <= '9') {
            var tx = x.TrimStart('0');
            var ty = y.TrimStart('0');

            var result = tx.Length.CompareTo(ty.Length);

            if (result != 0)
                return result;

            result = string.CompareOrdinal(tx, ty);

            if (result != 0)
                return result;
        }

        return string.CompareOrdinal(x, y);
    }

    public int Compare(string? x, string? y) {
        if (ReferenceEquals(x, y))
            return 0;
        if (x is null)
            return -1;
        if (y is null)
            return +1;

        x = x.ToLower();
        y = y.ToLower();
        var itemsX = Regex
            .Split(x, "([0-9]+)")
            .Where(item => !string.IsNullOrEmpty(item))
            .ToList();

        var itemsY = Regex
            .Split(y, "([0-9]+)")
            .Where(item => !string.IsNullOrEmpty(item))
            .ToList();

        for (var i = 0; i < Math.Min(itemsX.Count, itemsY.Count); ++i) {
            var result = CompareChunks(itemsX[i], itemsY[i]);

            if (result != 0)
                return result;
        }

        return itemsX.Count.CompareTo(itemsY.Count);
    }
}