using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Linq;
using VMFGenerator;

namespace VMFConverter
{
    public class PolygonTriangulator
    {

        public static List<List<Vector2>> Triangulate(List<Vector2> Polygon)
        {
            //for (int i = 0; i < Polygon.Count; i++)
            //{
            //    Polygon[i] /= 128;
            //}
            List<Vector2> sharedValues = Polygon.GroupBy(x => x).Where(g => g.Count() > 1).Select(x => x.Key).ToList();

            float amountToRaise = 0.1f;
            List<int> higherIndices = new List<int>();

            Dictionary<Vector2, int> highestIndex = new Dictionary<Vector2, int>();
            Dictionary<Vector2, float> currentHighest = new Dictionary<Vector2, float>();
            List<Vector2> onesToReset = new List<Vector2>();

            for (int i = 0; i < sharedValues.Count; i++)
            {
                highestIndex.Add(sharedValues[i], -1);
                currentHighest.Add(sharedValues[i], 0);
            }

            for (int i = 0; i < Polygon.Count; i++)
            {
                if (!sharedValues.Contains(Polygon[i]))
                {
                    continue;
                }

                int indexA = (i - 1) % Polygon.Count;
                if (indexA < 0)
                {
                    indexA = Polygon.Count - 1;
                }
                int indexB = (i) % Polygon.Count;
                int indexC = (i + 1) % Polygon.Count;

                Vector2 prev = Polygon[indexA];
                Vector2 curr = Polygon[indexB];
                Vector2 next = Polygon[indexC];

                if ((prev.Y + next.Y) > currentHighest[Polygon[i]])
                {
                    currentHighest[Polygon[i]] = (prev.Y + next.Y);
                    highestIndex[Polygon[i]] = i;
                }
            }

            foreach (Vector2 pos in highestIndex.Keys)
            {
                Polygon[highestIndex[pos]] = new Vector2(Polygon[highestIndex[pos]].X, Polygon[highestIndex[pos]].Y + amountToRaise);
                onesToReset.Add(Polygon[highestIndex[pos]]);
            }

            List<Vector2> remainingValues = new List<Vector2>(Polygon);
            List<List<Vector2>> result = new List<List<Vector2>>();

            int c2 = 0;
            int c = 0;
            int index = -1;
            bool triangleMade = true;
            while (triangleMade)
            {
                triangleMade = false;
                for (int v = Polygon.Count + 2; v >= 0; v--)
                {
                    index = v;

                    int indexA = (index - 1) % Polygon.Count;
                    if (indexA < 0)
                    {
                        indexA = Polygon.Count - 1;
                    }
                    int indexB = (index) % Polygon.Count;
                    int indexC = (index + 1) % Polygon.Count;
                    Vector2 prev = Polygon[indexA];
                    Vector2 curr = Polygon[indexB];
                    Vector2 next = Polygon[indexC];

                    Vector2 up = new Vector2(0, 1);

                    Vector2 abNormal = Vector2.Normalize(Shape.GetNormal2D(prev, curr));
                    Vector2 bcNormal = Vector2.Normalize(Shape.GetNormal2D(curr, next));
                    //Vector2 vertexNormalabcInner = -Vector2.Normalize((abNormal + bcNormal)) * 100;
                    //
                    //Vector2 abDir = Vector2.Normalize((prev + abNormal) - (curr + vertexNormalabcInner));
                    //Vector2 bcDir = Vector2.Normalize((next + abNormal) - (curr + vertexNormalabcInner));
                    //
                    //float distanceBetweenPrevNext = Vector2.Distance(prev, next);
                    //float distanceExtended = Vector2.Distance(prev + abNormal, next + bcNormal);

                    VMFDebug.CreateDebugImage("TriangulationStepAttempt" + c2, onDraw: (g) =>
                    {
                        float scale = 0.2f;
                        Point positionAdjustment = new Point(0, 0);
                        Pen whitePen = new Pen(Color.White, 3);
                        Pen greyPen = new Pen(Color.Gray, 3);
                        Pen blackPen = new Pen(Color.Black, 3);
                        Pen redPen = new Pen(Color.Red, 3);
                        Pen bluePen = new Pen(Color.Blue, 3);
                        Pen greenPen = new Pen(Color.Green, 3);
                        Pen otherPen = new Pen(Color.LightBlue, 3);

                        for (int i = 0; i < Polygon.Count; i++)
                        {
                            int iN = (i + 1) % Polygon.Count;
                            Point p1 = new Point((int)(Polygon[i].X * scale + positionAdjustment.X), (int)(Polygon[i].Y * scale + positionAdjustment.Y));
                            Point p2 = new Point((int)(Polygon[iN].X * scale + positionAdjustment.X), (int)(Polygon[iN].Y * scale + positionAdjustment.Y));

                            g.DrawLine(greyPen, p1, p2);
                        }

                        for (int i = 0; i < result.Count; i++)
                        {
                            for (int j = 0; j < result[i].Count; j++)
                            {
                                int iN = (j + 1) % result[i].Count;
                                Point p1 = new Point((int)(result[i][j].X * scale + positionAdjustment.X), (int)(result[i][j].Y * scale + positionAdjustment.Y));
                                Point p2 = new Point((int)(result[i][iN].X * scale + positionAdjustment.X), (int)(result[i][iN].Y * scale + positionAdjustment.Y));

                                g.DrawLine(blackPen, p1, p2);
                            }
                        }

                        Point prevP = new Point((int)(prev.X * scale + positionAdjustment.X), (int)(prev.Y * scale + positionAdjustment.Y));
                        Point currP = new Point((int)(curr.X * scale + positionAdjustment.X), (int)(curr.Y * scale + positionAdjustment.Y));
                        Point nextP = new Point((int)(next.X * scale + positionAdjustment.X), (int)(next.Y * scale + positionAdjustment.Y));
                        g.DrawLine(otherPen, prevP, currP);
                        g.DrawLine(otherPen, currP, nextP);
                        g.DrawLine(otherPen, nextP, prevP);

                        //Point ver = new Point((int)((curr + vertexNormalabcInner).X * scale) + positionAdjustment.Y, (int)((curr + vertexNormalabcInner).Y * scale) + positionAdjustment.Y);

                        //g.DrawLine(redPen, new Point((int)(curr.X * scale) + positionAdjustment.X, (int)(curr.Y * scale) + positionAdjustment.Y), ver);
                        g.DrawLine(bluePen,
                            new Point((int)(prev.X * scale) + positionAdjustment.X, (int)(prev.Y * scale) + positionAdjustment.Y),
                            new Point((int)(prev.X * scale) + (int)(abNormal.X * 30) + positionAdjustment.X, (int)(prev.Y * scale) + (int)(abNormal.Y * 30) + positionAdjustment.Y));
                        g.DrawLine(bluePen,
                            new Point((int)(next.X * scale) + positionAdjustment.X, (int)(next.Y * scale) + positionAdjustment.Y),
                            new Point((int)(next.X * scale) + (int)(bcNormal.X * 30) + positionAdjustment.X, (int)(next.Y * scale) + (int)(bcNormal.Y * 30) + positionAdjustment.Y));

                        g.DrawEllipse(redPen, prev.X * scale + positionAdjustment.X, prev.Y * scale + positionAdjustment.Y, 10, 10);
                        g.DrawEllipse(bluePen, curr.X * scale + positionAdjustment.X, curr.Y * scale + positionAdjustment.Y, 10, 10);
                        g.DrawEllipse(greenPen, next.X * scale + positionAdjustment.X, next.Y * scale + positionAdjustment.Y, 10, 10);

                    });
                    c2++;

                    Vector2 v1 = next - curr;
                    Vector2 v2 = prev - curr;
                    float val = (v1.X * v2.Y) - (v1.Y * v2.X);

                    if(val <= 0)
                    {
                        continue;
                    }

                    //if (distanceExtended < distanceBetweenPrevNext)
                    //{
                    //    continue;
                    //}

                    bool noGood = false;
                    for (int i = 0; i < remainingValues.Count; i++)
                    {
                        Vector2 point = remainingValues[i];

                        if(sharedValues.Contains(new Vector2(point.X, point.Y - amountToRaise)))
                        {
                            point = new Vector2(point.X, point.Y - amountToRaise);
                        }

                        Vector2 currPoint = point;
                        Vector2 altCurrPoint = new Vector2(currPoint.X, currPoint.Y + amountToRaise);
                        if (currPoint == prev || currPoint == curr || currPoint == next ||
                            altCurrPoint == prev || altCurrPoint == curr || altCurrPoint == next)
                        {
                            continue;
                        }
                        if (PointInTriangle(remainingValues[i], prev, curr, next))
                        {
                            noGood = true;
                            break;
                        }
                    }
                    if (noGood)
                    {
                        continue;
                    }

                    result.Add(new List<Vector2>()
                    {
                        prev,
                        curr,
                        next
                    });
                    Polygon.Remove(curr);

                    triangleMade = true;

                    if(Polygon.Count > 2)
                    {
                        VMFDebug.CreateDebugImage("TriangulationStep" + c, onDraw: (g) =>
                        {
                            float scale = 0.2f;
                            Point positionAdjustment = new Point(0, 0);
                            Pen whitePen = new Pen(Color.White, 3);
                            Pen greyPen = new Pen(Color.Gray, 3);
                            Pen blackPen = new Pen(Color.Black, 3);
                            Pen redPen = new Pen(Color.Red, 3);
                            Pen bluePen = new Pen(Color.Blue, 3);
                            Pen greenPen = new Pen(Color.Green, 3);

                            for (int i = 0; i < Polygon.Count; i++)
                            {
                                int iN = (i + 1) % Polygon.Count;
                                Point p1 = new Point((int)(Polygon[i].X * scale + positionAdjustment.X), (int)(Polygon[i].Y * scale + positionAdjustment.Y));
                                Point p2 = new Point((int)(Polygon[iN].X * scale + positionAdjustment.X), (int)(Polygon[iN].Y * scale + positionAdjustment.Y));

                                g.DrawLine(greyPen, p1, p2);
                            }

                            for (int i = 0; i < result.Count; i++)
                            {
                                for (int j = 0; j < result[i].Count; j++)
                                {
                                    int iN = (j + 1) % result[i].Count;
                                    Point p1 = new Point((int)(result[i][j].X * scale + positionAdjustment.X), (int)(result[i][j].Y * scale + positionAdjustment.Y));
                                    Point p2 = new Point((int)(result[i][iN].X * scale + positionAdjustment.X), (int)(result[i][iN].Y * scale + positionAdjustment.Y));

                                    g.DrawLine(blackPen, p1, p2);
                                }
                            }

                            //Point ver = new Point((int)((curr + vertexNormalabcInner).X * scale) + positionAdjustment.Y, (int)((curr + vertexNormalabcInner).Y * scale) + positionAdjustment.Y);

                            //g.DrawLine(redPen, new Point((int)(curr.X * scale) + positionAdjustment.X, (int)(curr.Y * scale) + positionAdjustment.Y), ver);
                            g.DrawLine(bluePen,
                                new Point((int)(prev.X * scale) + positionAdjustment.X, (int)(prev.Y * scale) + positionAdjustment.Y),
                                new Point((int)(prev.X * scale) + (int)(abNormal.X * 30) + positionAdjustment.X, (int)(prev.Y * scale) + (int)(abNormal.Y * 30) + positionAdjustment.Y));
                            g.DrawLine(bluePen,
                                new Point((int)(next.X * scale) + positionAdjustment.X, (int)(next.Y * scale) + positionAdjustment.Y),
                                new Point((int)(next.X * scale) + (int)(bcNormal.X * 30) + positionAdjustment.X, (int)(next.Y * scale) + (int)(bcNormal.Y * 30) + positionAdjustment.Y));

                            g.DrawEllipse(redPen, prev.X * scale + positionAdjustment.X, prev.Y * scale + positionAdjustment.Y, 10, 10);
                            g.DrawEllipse(bluePen, curr.X * scale + positionAdjustment.X, curr.Y * scale + positionAdjustment.Y, 10, 10);
                            g.DrawEllipse(greenPen, next.X * scale + positionAdjustment.X, next.Y * scale + positionAdjustment.Y, 10, 10);

                            Font font = new Font(FontFamily.GenericSansSerif, 20, SystemFonts.DefaultFont.Style);
                            for (int i = 0; i < remainingValues.Count; i++)
                            {
                                Point p1 = new Point((int)(remainingValues[i].X * scale + positionAdjustment.X), (int)(remainingValues[i].Y * scale + positionAdjustment.Y));
                                bool contains = false;
                                for (int j = 0; j < result.Count; j++)
                                {
                                    contains = result[j].Contains(new Vector2(remainingValues[i].X, remainingValues[i].Y));
                                    if(contains)
                                    {
                                        break;
                                    }
                                }
                                if (remainingValues[i] != prev && remainingValues[i] != curr && remainingValues[i] != next && !contains)
                                {
                                    g.DrawString(i.ToString(), font, Brushes.DarkGreen, new PointF(p1.X, p1.Y));
                                }
                            }
                            for (int i = 0; i < remainingValues.Count; i++)
                            {
                                Point p1 = new Point((int)(remainingValues[i].X * scale + positionAdjustment.X), (int)(remainingValues[i].Y * scale + positionAdjustment.Y));
                                if (remainingValues[i] == prev || remainingValues[i] == curr || remainingValues[i] == next)
                                {
                                    g.DrawString(i.ToString(), font, Brushes.DarkViolet, new PointF(p1.X + 40, p1.Y));
                                }
                            }

                        });
                    }

                    c++;
                }
            }

            for (int i = 0; i < result.Count; i++)
            {
                for (int j = 0; j < result[i].Count; j++)
                {
                    if (onesToReset.Contains(result[i][j]))
                    {
                        result[i][j] = new Vector2(result[i][j].X, result[i][j].Y - amountToRaise);
                    }
                }
            }

            for (int r = 0; r < result.Count; r++)
            {
                VMFDebug.CreateDebugImage("FinalTriangulation" + r, onDraw: (g) =>
                {
                    float scale = 0.2f;
                    Pen blackPen = new Pen(Color.Black, 3);
                    Pen greyPen = new Pen(Color.Gray, 3);

                    for (int i = 0; i < result.Count; i++)
                    {
                        for (int j = 0; j < result[i].Count; j++)
                        {
                            int iN = (j + 1) % result[i].Count;
                            Point p1 = new Point((int)(result[i][j].X * scale), (int)(result[i][j].Y * scale));
                            Point p2 = new Point((int)(result[i][iN].X * scale), (int)(result[i][iN].Y * scale));

                            g.DrawLine(greyPen, p1, p2);
                        }
                    }

                    for (int i = 0; i < result[r].Count; i++)
                    {
                        int iN = (i + 1) % result[r].Count;
                        Point p1 = new Point((int)(result[r][i].X * scale), (int)(result[r][i].Y * scale));
                        Point p2 = new Point((int)(result[r][iN].X * scale), (int)(result[r][iN].Y * scale));

                        g.DrawLine(blackPen, p1, p2);
                    }
                });
            }


            return result;
        }

        private static float sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
        }

        private static bool PointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            float d1, d2, d3;
            bool has_neg, has_pos;

            d1 = sign(pt, v1, v2);
            d2 = sign(pt, v2, v3);
            d3 = sign(pt, v3, v1);

            has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(has_neg && has_pos);
        }
    }
}

public class Delauney
{
    public static List<Vector2> Run(List<Vector2> points)
    {
        List<Vector2> result = new List<Vector2>();

        int n = 0;
        for (int x = 0; x < 500; x++)
        {
            for (int y = 0; y < 500; y++)
            {
                n = 0;
                int nX = (int)points[n].X;
                int nY = (int)points[n].Y;
                for (byte i = 0; i < 10; i++)
                {
                    int cX = (int)points[i].X;
                    int cY = (int)points[i].Y;
                    if (Vector2.Distance(new Vector2(cX, cY), new Vector2(x, y)) < Vector2.Distance(new Vector2(nX, nY), new Vector2(x, y)))
                        n = i;
                    nX = (int)points[n].X;
                    nY = (int)points[n].Y;
                }

                result.Add(new Vector2(nX, nY));
            }
        }

        return result;
    }
}