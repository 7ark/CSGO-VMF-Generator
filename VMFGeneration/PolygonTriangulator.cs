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
        /// <summary>
        /// My triangulation function. I used this https://www.gamedev.net/tutorials/programming/graphics/polygon-triangulation-r3334/ 
        /// as a guide and it was incredibly helpful. I also made my own modifications for my own usecases and attempts at fixes.
        /// </summary>
        /// <param name="Polygon"></param>
        /// <returns></returns>
        public static List<List<Vector2>> Triangulate(List<Vector2> Polygon)
        {
            //These are purely dummy values used for debug drawing
            int c2 = 0;
            int c = 0;

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

            //So this is all my attempt to bypass issues with connected polygons. When I have two edges that are right next to each other, and share points
            //I detect those, and one set I move *slightly* higher. That way the algorithm will treat them separately and doesnt get confused.
            //Then when its done all it need to do, I simply move them back down. It works okay enough? Some work arounds were needed but its working.
            foreach (Vector2 pos in highestIndex.Keys)
            {
                Polygon[highestIndex[pos]] = new Vector2(Polygon[highestIndex[pos]].X, Polygon[highestIndex[pos]].Y + amountToRaise);
                onesToReset.Add(Polygon[highestIndex[pos]]);
            }

            List<Vector2> remainingValues = new List<Vector2>(Polygon);
            List<List<Vector2>> result = new List<List<Vector2>>();

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

                        g.DrawEllipse(redPen, prev.X * scale + positionAdjustment.X, prev.Y * scale + positionAdjustment.Y, 10, 10);
                        g.DrawEllipse(bluePen, curr.X * scale + positionAdjustment.X, curr.Y * scale + positionAdjustment.Y, 10, 10);
                        g.DrawEllipse(greenPen, next.X * scale + positionAdjustment.X, next.Y * scale + positionAdjustment.Y, 10, 10);

                    });
                    c2++;

                    //2D cross product or wedge product or whatever you wanna call it
                    Vector2 v1 = next - curr;
                    Vector2 v2 = prev - curr;
                    float val = (v1.X * v2.Y) - (v1.Y * v2.X);

                    if(val <= 0)
                    {
                        continue;
                    }

                    bool noGood = false;
                    for (int i = 0; i < remainingValues.Count; i++)
                    {
                        Vector2 point = remainingValues[i];

                        Vector2 pointButShort = new Vector2(point.X, MathF.Round(point.Y - amountToRaise, 1));
                        if (sharedValues.Contains(pointButShort))
                        {
                            point = pointButShort;
                        }

                        Vector2 currPoint = point;
                        Vector2 altCurrPoint = new Vector2(currPoint.X, MathF.Round(currPoint.Y + amountToRaise, 1));
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
                            float scale = 0.15f;
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

            //Resetting the shared points values
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

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
        }

        private static bool PointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            float d1, d2, d3;
            bool has_neg, has_pos;

            d1 = Sign(pt, v1, v2);
            d2 = Sign(pt, v2, v3);
            d3 = Sign(pt, v3, v1);

            has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(has_neg && has_pos);
        }
    }
}