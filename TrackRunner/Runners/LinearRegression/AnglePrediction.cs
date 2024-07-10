using Microsoft.ML.Data;

namespace TrackRunner.Runners.LinearRegression;

public sealed class AnglePrediction
{
    [ColumnName("Score")]
    public float AngleInDegrees { get; set; }
}
