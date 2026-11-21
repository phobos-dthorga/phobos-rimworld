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

    /// <summary>The side fly's length along the vehicle, in cells (Framework 0.217.0, authored: room for two swags side by side and a gap).</summary>
    public const int FlyAlong = 3;

    /// <summary>How far a closed tent beside the vehicle reaches out, walls included: the walled awning, and the fold-out camper (authored).</summary>
    public const int AwningRoomOut = 4, CamperOut = 5, CamperAlong = 6;

    /// <summary>The rear tent's depth behind the vehicle, walls included, and how far it stands out past each side (authored).</summary>
    public const int RearTentDeep = 5, RearTentPast = 1;

    /// <summary>The least length of a closed tent along the vehicle, walls included: two cells inside.</summary>
    public const int RoomLeastAlong = 4;

    /// <summary>A closed tent in the camp's frame: its rectangle, walls included, and its door, a wall cell that is no corner.</summary>
    public readonly struct Room
    {
        public readonly int x0, z0, x1, z1, doorX, doorZ;

        public Room(int x0, int z0, int x1, int z1, int doorX, int doorZ)
        {
            this.x0 = x0;
            this.z0 = z0;
            this.x1 = x1;
            this.z1 = z1;
            this.doorX = doorX;
            this.doorZ = doorZ;
        }

        /// <summary>The cells inside the walls.</summary>
        public int InsideCells => System.Math.Max(0, x1 - x0 - 1) * System.Math.Max(0, z1 - z0 - 1);
    }

    /// <summary>
    /// The rows the side fly covers, first and last: three from the rear, or from past a roof tent standing there. Null when the vehicle's side
    /// is too short to have any row left.
    /// </summary>
    public static (int first, int last)? FlyRows(int length, bool tentAtRear)
    {
        int first = tentAtRear ? TentAlong : 0;
        int last = System.Math.Min(length - 1, first + FlyAlong - 1);
        return first > last ? null : (first, last);
    }

    /// <summary>
    /// The walled awning: a closed canvas room along the camp's side, four cells out and as long as the vehicle (at least four cells), starting
    /// past a roof tent at the rear. Its door is in the middle of its outer wall.
    /// </summary>
    public static Room AwningRoom(int length, bool tentAtRear)
    {
        int first = tentAtRear ? TentAlong : 0;
        int along = System.Math.Max(length - first, RoomLeastAlong);
        int last = first + along - 1;
        return new Room(0, first, AwningRoomOut - 1, last, AwningRoomOut - 1, first + along / 2);
    }

    /// <summary>The fold-out camper: a closed room from the trailer's rear, five cells out and six along, its door in the outer wall beside the walkway.</summary>
    public static Room Camper() => new(0, 0, CamperOut - 1, CamperAlong - 1, CamperOut - 1, 2);

    /// <summary>The camper's two double beds in the camp's frame, each two by two, against the wall nearest the trailer; the outer column is the walkway.</summary>
    public static (int x0, int z0, int x1, int z1)[] CamperBeds() => new[] { (1, 1, 2, 2), (1, 3, 2, 4) };

    /// <summary>
    /// The rear tent: a closed room behind a vehicle <paramref name="width"/> cells wide, one cell past each of its sides and five deep, its door
    /// in the middle of the wall furthest from the vehicle. In the camp's frame the vehicle's own columns are −1 (nearest the camp's side) to
    /// −width, and rows behind its rear are negative.
    /// </summary>
    public static Room RearTent(int width)
    {
        int x0 = -(width + RearTentPast), x1 = RearTentPast - 1;
        return new Room(x0, -RearTentDeep, x1, -1, x0 + (x1 - x0) / 2, -RearTentDeep);
    }

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
    public static List<int> PoleRows(int length, bool tentAtRear) => PoleRowsBetween(length < 1 ? 1 : tentAtRear ? TentAlong : 0, length - 1);

    /// <summary>The rows where poles stand between two rows of the vehicle's side, both included: one at each end and more between.</summary>
    public static List<int> PoleRowsBetween(int first, int last)
    {
        var rows = new List<int>();
        if (first > last || last < 0) return rows;
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
