using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Vali.Core.Hash;

namespace Vali.Core;

public static class Hasher
{
    /// <summary>
    /// Encode latitude/longitude into a ulong geohash.
    /// Layout: bits 63-60 = precision (1-12), bits 59-0 = interleaved lat/lng bits, left-aligned.
    /// Sentinel value 0UL = "no hash".
    /// </summary>
    public static ulong Encode(double latitude, double longitude, HashPrecision precision)
    {
        var p = (int)precision;
        var totalBits = p * 5;
        var latBits = totalBits / 2;
        var lngBits = totalBits - latBits;

        var lat = (latitude + 90.0) / 180.0;
        var lng = (longitude + 180.0) / 360.0;

        var latVal = EncodeRange(lat, latBits);
        var lngVal = EncodeRange(lng, lngBits);

        var interleaved = Interleave(latVal, lngVal, totalBits);
        return ((ulong)p << 60) | (interleaved << (60 - totalBits));
    }

    public static ulong ParentOf(double latitude, double longitude, HashPrecision precision)
    {
        var hash = Encode(latitude, longitude, precision);
        return Parent(hash);
    }

    private static ulong Parent(ulong hash)
    {
        var p = (int)(hash >> 60);
        if (p <= 1)
        {
            return hash;
        }

        var parentP = p - 1;
        var parentTotalBits = parentP * 5;
        // Mask to keep only the top parentTotalBits of the 60-bit payload
        var shift = 60 - parentTotalBits;
        var payload = (hash & 0x0FFF_FFFF_FFFF_FFFFUL) >> shift << shift;
        return ((ulong)parentP << 60) | payload;
    }

    private static readonly ConcurrentDictionary<ulong, ulong[]> NeighborCache = new();

    public static ulong[] Neighbors(ulong hash)
    {
        if (NeighborCache.TryGetValue(hash, out var cached))
        {
            return cached;
        }

        var neighbors = ComputeNeighbors(hash);
        NeighborCache.TryAdd(hash, neighbors);
        return neighbors;
    }

    public static BoundingBox GetBoundingBox(ulong hash)
    {
        var p = (int)(hash >> 60);
        var totalBits = p * 5;
        var latBits = totalBits / 2;
        var lngBits = totalBits - latBits;

        var payload = hash & 0x0FFF_FFFF_FFFF_FFFFUL;
        var interleaved = payload >> (60 - totalBits);

        Deinterleave(interleaved, totalBits, out var latVal, out var lngVal);

        var latRange = 1UL << latBits;
        var lngRange = 1UL << lngBits;

        var minLat = (double)latVal / latRange * 180.0 - 90.0;
        var maxLat = (double)(latVal + 1) / latRange * 180.0 - 90.0;
        var minLng = (double)lngVal / lngRange * 360.0 - 180.0;
        var maxLng = (double)(lngVal + 1) / lngRange * 360.0 - 180.0;

        return new BoundingBox(minLat, maxLat, minLng, maxLng);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong EncodeRange(double normalized, int bits)
    {
        var range = 1UL << bits;
        var val = (ulong)(normalized * range);
        return val >= range ? range - 1 : val;
    }

    /// <summary>
    /// Spread a value's bits into even bit positions (Morton spread).
    /// Input: bits at positions 0..29. Output: bits at positions 0,2,4,...,58.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Spread(ulong x)
    {
        x &= 0x3FFF_FFFFUL;
        x = (x | (x << 16)) & 0x0000_FFFF_0000_FFFFUL;
        x = (x | (x << 8))  & 0x00FF_00FF_00FF_00FFUL;
        x = (x | (x << 4))  & 0x0F0F_0F0F_0F0F_0F0FUL;
        x = (x | (x << 2))  & 0x3333_3333_3333_3333UL;
        x = (x | (x << 1))  & 0x5555_5555_5555_5555UL;
        return x;
    }

    /// <summary>
    /// Compact even bit positions back into contiguous bits (inverse of Spread).
    /// Input: bits at positions 0,2,4,... Output: bits at positions 0..29.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Compact(ulong x)
    {
        x &= 0x5555_5555_5555_5555UL;
        x = (x | (x >> 1))  & 0x3333_3333_3333_3333UL;
        x = (x | (x >> 2))  & 0x0F0F_0F0F_0F0F_0F0FUL;
        x = (x | (x >> 4))  & 0x00FF_00FF_00FF_00FFUL;
        x = (x | (x >> 8))  & 0x0000_FFFF_0000_FFFFUL;
        x = (x | (x >> 16)) & 0x0000_0000_FFFF_FFFFUL;
        return x;
    }

    /// <summary>
    /// Geohash interleave: MSB is always a longitude bit.
    /// Even-precision (lngBits == latBits): lat at even LSB positions, lng shifted left by 1.
    /// Odd-precision (lngBits == latBits+1): lng at even LSB positions, lat shifted left by 1.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Interleave(ulong latVal, ulong lngVal, int totalBits)
    {
        return totalBits % 2 == 0
            ? Spread(latVal) | (Spread(lngVal) << 1)
            : Spread(lngVal) | (Spread(latVal) << 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Deinterleave(ulong interleaved, int totalBits, out ulong latVal, out ulong lngVal)
    {
        if (totalBits % 2 == 0)
        {
            latVal = Compact(interleaved);
            lngVal = Compact(interleaved >> 1);
        }
        else
        {
            lngVal = Compact(interleaved);
            latVal = Compact(interleaved >> 1);
        }
    }

    private static readonly int[] DLat = [1, 1, 0, -1, -1, -1, 0, 1];
    private static readonly int[] DLng = [0, 1, 1, 1, 0, -1, -1, -1];

    private static ulong[] ComputeNeighbors(ulong hash)
    {
        var p = (int)(hash >> 60);
        var totalBits = p * 5;
        var latBits = totalBits / 2;
        var lngBits = totalBits - latBits;

        var payload = hash & 0x0FFF_FFFF_FFFF_FFFFUL;
        var interleaved = payload >> (60 - totalBits);

        Deinterleave(interleaved, totalBits, out var latVal, out var lngVal);

        var latRange = 1UL << latBits;
        var lngRange = 1UL << lngBits;

        var result = new ulong[8];
        for (var i = 0; i < 8; i++)
        {
            var newLat = (long)latVal + DLat[i];
            var newLng = (long)lngVal + DLng[i];

            // Clamp latitude
            if (newLat < 0) newLat = 0;
            if (newLat >= (long)latRange) newLat = (long)latRange - 1;

            // Wrap longitude
            if (newLng < 0) newLng = (long)lngRange + newLng;
            if (newLng >= (long)lngRange) newLng -= (long)lngRange;

            var neighborInterleaved = Interleave((ulong)newLat, (ulong)newLng, totalBits);
            result[i] = ((ulong)p << 60) | (neighborInterleaved << (60 - totalBits));
        }

        return result;
    }
}
