using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Where a vehicle's camp goes (Framework 0.212.0; docs/development/vehicle-accessories.md, Y3): the geometry of the camp order, game-free and
/// tested. A camp is laid out in the vehicle's own frame on one side of it: <c>x</c> counts out from that side (0 is the column touching the
/// vehicle), <c>z</c> runs along it from its rearmost row (0) towards its front. Written from the vehicle's rectangle on the map, so a vehicle
/// of even width or length lands its camp against its side as one of odd size does.
/// </summary>
public static class CampLayoutRule
{
    /// <summary>A direction in the camp's frame: away from the vehicle, towards its front, towards it, towards its back.</summary>
    public const int Out = 0, Forward = 1, In = 2, Back = 3;

    /// <summary>The side a camp goes: the vehicle's left (as its driver sees it) or its right.</summary>
    public const int Left = 1, Right = -1;

    /// <summary>The awning's depth out from the vehicle, in cells (the record's two deep, authored).</summary>
    public const int AwningDepth = 2;

    /// <summary>The most cells between two awning poles along the vehicle (authored: a pole every few metres, as real awnings stand).</summary>
    public const int PoleSpacing = 6;

    /// <summary>The roof tent's footprint, along and out: a two-person bed (B1).</summary>
    public const int TentAlong = 2, TentOut = 2;

    /// <summary>A map direction as a cell step: 0 north, 1 east, 2 south, 3 west, as the game counts rotations.</summary>
    public static (int x, int z) Step(int rotation) => (((rotation % 4) + 4) % 4) switch
    {
        1 => (1, 0),
        2 => (0, -1),
        3 => (-1, 0),
        _ => (0, 1),
    };

    /// <summary>The camp's frame on one side of a vehicle standing on <paramref name="minX"/> to <paramref name="maxZ"/> and facing
    /// <paramref name="facing"/>.</summary>
    public sealed class Frame
    {
        public readonly int cornerX, cornerZ;
        public readonly int outX, outZ, forwardX, forwardZ;
        public readonly int facing, outRotation;
        /// <summary>The vehicle's length in cells, along its facing.</summary>
        public readonly int length;

        public Frame(int minX, int minZ, int maxX, int maxZ, int facing, int side)
        {
            this.facing = ((facing % 4) + 4) % 4;
            // Left of the facing is a quarter turn anticlockwise: the left of north is west.
            outRotation = side == Left ? (this.facing + 3) % 4 : (this.facing + 1) % 4;
            (forwardX, forwardZ) = Step(this.facing);
            (outX, outZ) = Step(outRotation);
            // The corner is the vehicle's rearmost cell on the camp's side: each axis is set by whichever of the two directions runs along it.
            cornerX = outX > 0 ? maxX : outX < 0 ? minX : forwardX > 0 ? minX : maxX;
            cornerZ = outZ > 0 ? maxZ : outZ < 0 ? minZ : forwardZ > 0 ? minZ : maxZ;
            length = forwardX != 0 ? maxX - minX + 1 : maxZ - minZ + 1;
        }

        /// <summary>A cell of the camp's frame on the map.</summary>
        public (int x, int z) ToMap(int x, int z) => (cornerX + outX * (x + 1) + forwardX * z, cornerZ + outZ * (x + 1) + forwardZ * z);

        /// <summary>A map cell in the camp's frame: the inverse of <see cref="ToMap"/>.</summary>
        public (int x, int z) FromMap(int mapX, int mapZ)
        {
            int dx = mapX - cornerX, dz = mapZ - cornerZ;
            return (dx * outX + dz * outZ - 1, dx * forwardX + dz * forwardZ);
        }

        /// <summary>A direction of the camp's frame as a map rotation.</summary>
        public int ToMapRotation(int frameDirection) => (((frameDirection % 4) + 4) % 4) switch
        {
            Forward => facing,
            In => (outRotation + 2) % 4,
            Back => (facing + 2) % 4,
            _ => outRotation,
        };

        /// <summary>A map rotation as a direction of the camp's frame: the inverse of <see cref="ToMapRotation"/>.</summary>
        public int FromMapRotation(int mapRotation)
        {
            for (int direction = 0; direction < 4; direction++)
            {
                if (ToMapRotation(direction) == ((mapRotation % 4) + 4) % 4) return direction;
            }
            return Out;
        }
    }

    /// <summary>
    /// The places a piece of camp gear may go, best first, as the frame cell of its corner nearest the vehicle's rear and side: in bands of its
    /// own depth out from the vehicle, each band from the rear forwards. A piece lies across the vehicle's side, its head towards it, so it
    /// takes <paramref name="along"/> cells along and <paramref name="outward"/> out. Nothing reaches past <paramref name="maxOut"/> cells out.
    /// </summary>
    public static IEnumerable<(int x, int z)> Slots(int length, int along, int outward, int maxOut)
    {
        if (along < 1 || outward < 1) yield break;
        for (int x = 0; x + outward <= maxOut; x += outward)
        {
            for (int z = 0; z + along <= System.Math.Max(length, along); z++) yield return (x, z);
        }
    }

    /// <summary>
    /// The rows along the vehicle where the awning's outer poles stand: one at each end and more between, none further apart than
    /// <see cref="PoleSpacing"/>. A roof tent at the rear holds that corner up itself, so the rear pole moves forward past it.
    /// </summary>
    public static List<int> PoleRows(int length, bool tentAtRear)
    {
        var rows = new List<int>();
        if (length < 1) return rows;
        int first = tentAtRear ? TentAlong : 0;
        int last = length - 1;
        if (first > last) return rows;
        rows.Add(first);
        if (last == first) return rows;
        int gaps = (last - first + PoleSpacing - 1) / PoleSpacing;
        for (int index = 1; index < gaps; index++) rows.Add(first + (last - first) * index / gaps);
        rows.Add(last);
        return rows;
    }

    /// <summary>Whether a roof tent fits beside a vehicle this long: it needs two rows of its side.</summary>
    public static bool TentFits(int length) => length >= TentAlong;
}
