using Microsoft.ML.Data;

namespace TrackRunner.Runners;

public sealed class PositionInfo : IEquatable<PositionInfo>
{
    [LoadColumn(0)]
    public float DistanceFront { get; set; } = float.MaxValue;

    [LoadColumn(1)]
    public float Direction { get; set; } = 1;

    [ColumnName("Label")]
    public float AngleInDegrees { get; set; }

    public bool Equals(PositionInfo? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return DistanceFront.Equals(other.DistanceFront)
            && Direction.Equals(other.Direction)
            && AngleInDegrees.Equals(other.AngleInDegrees);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((PositionInfo)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(DistanceFront, Direction, AngleInDegrees);
    }
}