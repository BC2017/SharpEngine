using System.Numerics;
using SharpEngine.World.Chunks;

namespace SharpEngine.World.Raycasting;

public static class VoxelRaycaster
{
    public static WorldVoxelRaycastHit? RaycastWorld(
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        Func<BlockPosition, bool> isSolid)
    {
        ArgumentNullException.ThrowIfNull(isSolid);

        if (direction.LengthSquared() <= float.Epsilon || maxDistance <= 0.0f)
        {
            return null;
        }

        direction = Vector3.Normalize(direction);

        int x = FloorToInt(origin.X);
        int y = FloorToInt(origin.Y);
        int z = FloorToInt(origin.Z);

        int stepX = Math.Sign(direction.X);
        int stepY = Math.Sign(direction.Y);
        int stepZ = Math.Sign(direction.Z);

        float tMaxX = InitialRayLength(origin.X, direction.X, x, stepX);
        float tMaxY = InitialRayLength(origin.Y, direction.Y, y, stepY);
        float tMaxZ = InitialRayLength(origin.Z, direction.Z, z, stepZ);

        float tDeltaX = StepRayLength(direction.X);
        float tDeltaY = StepRayLength(direction.Y);
        float tDeltaZ = StepRayLength(direction.Z);

        BlockPosition previous = new(x, y, z);
        int normalX = 0;
        int normalY = 0;
        int normalZ = 0;

        while (true)
        {
            BlockPosition current = new(x, y, z);
            if (isSolid(current))
            {
                return new WorldVoxelRaycastHit(current, previous, normalX, normalY, normalZ);
            }

            previous = current;

            if (tMaxX < tMaxY)
            {
                if (tMaxX < tMaxZ)
                {
                    if (tMaxX > maxDistance)
                    {
                        return null;
                    }

                    x += stepX;
                    normalX = -stepX;
                    normalY = 0;
                    normalZ = 0;
                    tMaxX += tDeltaX;
                }
                else
                {
                    if (tMaxZ > maxDistance)
                    {
                        return null;
                    }

                    z += stepZ;
                    normalX = 0;
                    normalY = 0;
                    normalZ = -stepZ;
                    tMaxZ += tDeltaZ;
                }
            }
            else if (tMaxY < tMaxZ)
            {
                if (tMaxY > maxDistance)
                {
                    return null;
                }

                y += stepY;
                normalX = 0;
                normalY = -stepY;
                normalZ = 0;
                tMaxY += tDeltaY;
            }
            else
            {
                if (tMaxZ > maxDistance)
                {
                    return null;
                }

                z += stepZ;
                normalX = 0;
                normalY = 0;
                normalZ = -stepZ;
                tMaxZ += tDeltaZ;
            }
        }
    }

    public static VoxelRaycastHit? Raycast(
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        Func<LocalBlockPosition, bool> isSolid)
    {
        ArgumentNullException.ThrowIfNull(isSolid);

        if (direction.LengthSquared() <= float.Epsilon || maxDistance <= 0.0f)
        {
            return null;
        }

        direction = Vector3.Normalize(direction);

        int x = FloorToInt(origin.X);
        int y = FloorToInt(origin.Y);
        int z = FloorToInt(origin.Z);

        int stepX = Math.Sign(direction.X);
        int stepY = Math.Sign(direction.Y);
        int stepZ = Math.Sign(direction.Z);

        float tMaxX = InitialRayLength(origin.X, direction.X, x, stepX);
        float tMaxY = InitialRayLength(origin.Y, direction.Y, y, stepY);
        float tMaxZ = InitialRayLength(origin.Z, direction.Z, z, stepZ);

        float tDeltaX = StepRayLength(direction.X);
        float tDeltaY = StepRayLength(direction.Y);
        float tDeltaZ = StepRayLength(direction.Z);

        LocalBlockPosition previous = new(x, y, z);
        int normalX = 0;
        int normalY = 0;
        int normalZ = 0;

        while (true)
        {
            LocalBlockPosition current = new(x, y, z);
            if (IsInsideChunk(x, y, z) && isSolid(current))
            {
                return new VoxelRaycastHit(current, previous, normalX, normalY, normalZ);
            }

            previous = current;

            if (tMaxX < tMaxY)
            {
                if (tMaxX < tMaxZ)
                {
                    if (tMaxX > maxDistance)
                    {
                        return null;
                    }

                    x += stepX;
                    normalX = -stepX;
                    normalY = 0;
                    normalZ = 0;
                    tMaxX += tDeltaX;
                }
                else
                {
                    if (tMaxZ > maxDistance)
                    {
                        return null;
                    }

                    z += stepZ;
                    normalX = 0;
                    normalY = 0;
                    normalZ = -stepZ;
                    tMaxZ += tDeltaZ;
                }
            }
            else if (tMaxY < tMaxZ)
            {
                if (tMaxY > maxDistance)
                {
                    return null;
                }

                y += stepY;
                normalX = 0;
                normalY = -stepY;
                normalZ = 0;
                tMaxY += tDeltaY;
            }
            else
            {
                if (tMaxZ > maxDistance)
                {
                    return null;
                }

                z += stepZ;
                normalX = 0;
                normalY = 0;
                normalZ = -stepZ;
                tMaxZ += tDeltaZ;
            }
        }
    }

    private static bool IsInsideChunk(int x, int y, int z)
    {
        return x is >= 0 and < Chunk.Size &&
            y is >= 0 and < Chunk.Height &&
            z is >= 0 and < Chunk.Size;
    }

    private static int FloorToInt(float value)
    {
        return (int)MathF.Floor(value);
    }

    private static float InitialRayLength(float origin, float direction, int voxel, int step)
    {
        if (step == 0)
        {
            return float.PositiveInfinity;
        }

        float nextBoundary = step > 0 ? voxel + 1.0f : voxel;
        return (nextBoundary - origin) / direction;
    }

    private static float StepRayLength(float direction)
    {
        return direction == 0.0f ? float.PositiveInfinity : MathF.Abs(1.0f / direction);
    }
}
