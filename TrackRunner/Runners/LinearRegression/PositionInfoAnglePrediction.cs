using Microsoft.ML.Data;

namespace TrackRunner.Runners.LinearRegression;

public sealed class PositionInfoAnglePrediction
{
    [ColumnName("Score")]
    public float AngleInDegrees { get; set; }
}