using System.Windows;
using System.Windows.Shapes;

namespace TrackRunner.Runners;

public static class GeometryOperations
{
    public static Point? Intersect((Point start, Point end) l1, (Point start, Point end) l2)
    {
        double firstLineSlopeX = l1.end.X - l1.start.X;
        double firstLineSlopeY = l1.end.Y - l1.start.Y;

        double secondLineSlopeX = l2.end.X - l2.start.X;
        double secondLineSlopeY = l2.end.Y - l2.start.Y;

        double s = (-firstLineSlopeY * (l1.start.X - l2.start.X) + firstLineSlopeX * (l1.start.Y - l2.start.Y)) / (-secondLineSlopeX * firstLineSlopeY + firstLineSlopeX * secondLineSlopeY);
        double t = (secondLineSlopeX * (l1.start.Y - l2.start.Y) - secondLineSlopeY * (l1.start.X - l2.start.X)) / (-secondLineSlopeX * firstLineSlopeY + firstLineSlopeX * secondLineSlopeY);

        if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
        {
            double intersectionPointX = l1.start.X + t * firstLineSlopeX;
            double intersectionPointY = l1.start.Y + t * firstLineSlopeY;

            return new Point(intersectionPointX, intersectionPointY);
        }

        return null; // No collision
    }

    public static float? GetRayToLineIntersectionDistance(Point rayOrigin, Vector rayDirection, (Point start, Point end) line)
    {
        Vector v1 = rayOrigin - line.start;
        Vector v2 = line.end - line.start;
        var v3 = new Vector(-rayDirection.Y, rayDirection.X);

        double dot = v2 * v3;
        if (Math.Abs(dot) < 0.000001)
        {
            return null;
        }

        double t1 = Vector.CrossProduct(v2, v1) / dot;
        double t2 = v1 * v3 / dot;

        if (t1 >= 0.0 && t2 >= 0.0 && t2 <= 1.0)
        {
            return (float?)t1;
        }

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
