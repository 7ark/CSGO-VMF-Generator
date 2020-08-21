using System;
using System.Collections.Generic;
using System.Drawing;
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
        private struct Line
        {
            public Vector2 p1, p2;
        };

        private bool OnLine(Line line1, Vector2 p)
        {   //check whether p is on the line or not
            if (p.X <= MathF.Max(line1.p1.X, line1.p2.X) && p.X <= MathF.Min(line1.p1.X, line1.p2.X) &&
               (p.Y <= MathF.Max(line1.p1.Y, line1.p2.Y) && p.Y <= MathF.Min(line1.p1.Y, line1.p2.Y)))
            {
                return true;
            }

            return false;
        }

        private int Direction(Vector2 a, Vector2 b, Vector2 c)
        {
            int val = (int)((b.Y - a.Y) * (c.X - b.X) - (b.X - a.X) * (c.Y - b.Y));
            if (val == 0)
            {
                return 0;
            }
            else if (val < 0)
            {
                //CCW direction
                return 2;
            }
            else
            {
                //CW direction
                return 1;
            }
        }

        private bool IsIntersect(Line line1, Line line2)
        {
            int dir1 = Direction(line1.p1, line1.p2, line2.p1);
            int dir2 = Direction(line1.p1, line1.p2, line2.p2);
            int dir3 = Direction(line2.p1, line2.p2, line1.p1);
            int dir4 = Direction(line2.p1, line2.p2, line1.p2);

            if (dir1 != dir2 && dir3 != dir4)
                return true; //Intersecting

            if (dir1 == 0 && OnLine(line1, line2.p1)) //When p2 of line2 are on the line1
                return true;

            if (dir2 == 0 && OnLine(line1, line2.p2)) //When p1 of line2 are on the line1
                return true;

            if (dir3 == 0 && OnLine(line2, line1.p1)) //When p2 of line1 are on the line2
                return true;

            if (dir4 == 0 && OnLine(line2, line1.p2)) //When p1 of line1 are on the line2
                return true;

            return false;
        }

        public bool Intersects(LineEquation otherLine)
        {
            return IsIntersect(new Line() { p1 = Start, p2 = End }, new Line() { p1 = otherLine.Start, p2 = otherLine.End });
        }


        public bool IntersectsWithLine(LineEquation otherLine, out Vector2 intersectionPoint)
        {
            intersectionPoint = new Vector2(0, 0);

            if(!IsIntersect(new Line() { p1 = Start, p2 = End }, new Line() { p1 = otherLine.Start, p2 = otherLine.End }))
            {
                return false;
            }

            if (IsVertical && otherLine.IsVertical)
            {
                return false;
            }
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
