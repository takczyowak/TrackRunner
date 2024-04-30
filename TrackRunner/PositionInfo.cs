namespace TrackRunner
{
    public class PositionInfo
    {
        public double DistanceFront { get; set; } = double.MaxValue;
        public double DistanceLeft { get; set; } = double.MaxValue;
        public double DistanceRight { get; set; } = double.MaxValue;
        public double AngleInDegrees { get; set; }
    }
}
