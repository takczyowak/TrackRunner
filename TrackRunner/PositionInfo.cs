using Microsoft.ML.Data;

namespace TrackRunner
{
    public class PositionInfo : IEquatable<PositionInfo>
    {
        [LoadColumn(0)]
        public float DistanceFront { get; set; } = float.MaxValue;

        [LoadColumn(1)]
        public float DistanceLeft { get; set; } = float.MaxValue;

        [LoadColumn(2)]
        public float DistanceRight { get; set; } = float.MaxValue;

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
                && DistanceLeft.Equals(other.DistanceLeft)
                && DistanceRight.Equals(other.DistanceRight)
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
            return HashCode.Combine(DistanceFront, DistanceLeft, DistanceRight, AngleInDegrees);
        }
    }
}
