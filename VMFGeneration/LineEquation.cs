using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace VMFConverter
{
    public class LineEquation
    {
        public LineEquation(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;

            IsVertical = Math.Abs(End.X - start.X) < 0.00001f;
            M = (End.Y - Start.Y) / (End.X - Start.X);
            A = -M;
            B = 1;
            C = Start.Y - M * Start.X;
        }

        public bool IsVertical { get; private set; }

        public float M { get; private set; }

        public Vector2 Start { get; private set; }
        public Vector2 End { get; private set; }

        public float A { get; private set; }
        public float B { get; private set; }
        public float C { get; private set; }

        public bool IntersectsWithLine(LineEquation otherLine, out Vector2 intersectionPoint)
        {
            intersectionPoint = new Vector2(0, 0);
            if (IsVertical && otherLine.IsVertical)
                return false;
            if (IsVertical || otherLine.IsVertical)
            {
                intersectionPoint = GetIntersectionPointIfOneIsVertical(otherLine, this);
                return true;
            }
            float delta = A * otherLine.B - otherLine.A * B;
            bool hasIntersection = Math.Abs(delta - 0) > 0.0001f;
            if (hasIntersection)
            {
                float x = (otherLine.B * C - B * otherLine.C) / delta;
                float y = (A * otherLine.C - otherLine.A * C) / delta;
                intersectionPoint = new Vector2(x, y);
            }
            return hasIntersection;
        }

        private static Vector2 GetIntersectionPointIfOneIsVertical(LineEquation line1, LineEquation line2)
        {
            LineEquation verticalLine = line2.IsVertical ? line2 : line1;
            LineEquation nonVerticalLine = line2.IsVertical ? line1 : line2;

            float y = (verticalLine.Start.X - nonVerticalLine.Start.X) *
                       (nonVerticalLine.End.Y - nonVerticalLine.Start.Y) /
                       ((nonVerticalLine.End.X - nonVerticalLine.Start.X)) +
                       nonVerticalLine.Start.Y;
            float x = line1.IsVertical ? line1.Start.X : line2.Start.X;
            return new Vector2(x, y);
        }

        public override string ToString()
        {
            return "[" + Start + "], [" + End + "]";
        }
    }
}
