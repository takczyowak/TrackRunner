using Microsoft.ML.Data;

namespace TrackRunner
{
    public class PositionInfoAnglePrediction
    {
        [ColumnName("Score")]
        public float AngleInDegrees { get; set; }
    }
}
