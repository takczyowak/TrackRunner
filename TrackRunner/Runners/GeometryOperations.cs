using System.Windows;
using System.Windows.Shapes;

namespace TrackRunner.Runners;

public static class GeometryOperations
{
    public static Point? Intersect(Line l1, Line l2)
    {
        double firstLineSlopeX = l1.X2 - l1.X1;
        double firstLineSlopeY = l1.Y2 - l1.Y1;

        double secondLineSlopeX = l2.X2 - l2.X1;
        double secondLineSlopeY = l2.Y2 - l2.Y1;

        double s = (-firstLineSlopeY * (l1.X1 - l2.X1) + firstLineSlopeX * (l1.Y1 - l2.Y1)) / (-secondLineSlopeX * firstLineSlopeY + firstLineSlopeX * secondLineSlopeY);
        double t = (secondLineSlopeX * (l1.Y1 - l2.Y1) - secondLineSlopeY * (l1.X1 - l2.X1)) / (-secondLineSlopeX * firstLineSlopeY + firstLineSlopeX * secondLineSlopeY);

        if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
        {
            double intersectionPointX = l1.X1 + (t * firstLineSlopeX);
            double intersectionPointY = l1.Y1 + (t * firstLineSlopeY);

            return new Point(intersectionPointX, intersectionPointY);
        }

        return null; // No collision
    }

    public static float? GetRayToLineIntersectionDistance(Point rayOrigin, Vector rayDirection, Line line) 
    {
        var p1 = new Point(line.X1, line.Y1);
        var p2 = new Point(line.X2, line.Y2);
        var v1 = rayOrigin - p1;
        var v2 = p2 - p1;
        var v3 = new Vector(-rayDirection.Y, rayDirection.X);


        var dot = v2 * v3;
        if (Math.Abs(dot) < 0.000001)
            return null;

        var t1 = Vector.CrossProduct(v2, v1) / dot;
        var t2 = (v1 * v3) / dot;

        if (t1 >= 0.0 && (t2 >= 0.0 && t2 <= 1.0))
            return (float?)t1;

        return null;
    }

    public static Point RotatePoint(Point pointToRotate, Point centerPoint, float angleInDegrees)
    {
        double angleInRadians = angleInDegrees * (Math.PI / 180);
        double cosTheta = Math.Cos(angleInRadians);
        double sinTheta = Math.Sin(angleInRadians);
        return new Point
        {
            X = cosTheta * (pointToRotate.X - centerPoint.X) - sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X,
            Y = sinTheta * (pointToRotate.X - centerPoint.X) + cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y
        };
    }
}