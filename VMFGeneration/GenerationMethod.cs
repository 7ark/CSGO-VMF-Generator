using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Principal;
using VMFGenerator;

namespace VMFConverter
{
    public class ImageGenerationMethod : GenerationMethod
    {
        private struct PixelFlat
        {
            public Point Point;
            public float FlatnessValue;
        }

        public string InputFilePath;

        public override List<Shape> GetBrushes()
        {
            List<Shape> shapes = new List<Shape>();

            if(!File.Exists(InputFilePath))
            {
                Console.WriteLine("ERROR: Given file path " + InputFilePath + " does not exist! Not running image generation.");
                return shapes;
            }

            List<Vector2> edgePositions = new List<Vector2>();
            Bitmap map = new Bitmap(InputFilePath);
            map.RotateFlip(RotateFlipType.Rotate180FlipX);
            map = new Bitmap(map, new Size(map.Width * 2, map.Height * 2));
            int[,] grid = new int[map.Width, map.Height];
            int[,] testingGrid = new int[map.Width, map.Height];

            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    grid[x, y] = map.GetPixel(x, y).G > 50 ? 1 : 0;
                    testingGrid[x, y] = grid[x, y];
                }
            }

            List<List<Point>> emptySpots = new List<List<Point>>();

            //Finds all the space between the white space
            while (true)
            {
                Point nextEmptyArea = new Point();
                bool foundSpot = false;
                for (int x = 0; x < testingGrid.GetLength(0); x++)
                {
                    for (int y = 0; y < testingGrid.GetLength(1); y++)
                    {
                        if(testingGrid[x, y] == 0)
                        {
                            nextEmptyArea = new Point(x, y);
                            foundSpot = true;
                            break;
                        }
                    }
                    if(foundSpot)
                    {
                        break;
                    }
                }

                if(foundSpot)
                {
                    emptySpots.Add(GetEmptySpace(ref testingGrid, nextEmptyArea.X, nextEmptyArea.Y));
                }
                else
                {
                    break;
                }
            }

            if(emptySpots.Count > 1)
            {

                //Gets just the bottom edge of the empty space
                List<List<Point>> allBottomEdges = new List<List<Point>>();
                for (int i = 0; i < emptySpots.Count; i++)
                {
                    List<Point> bottomEdges = new List<Point>();
                    for (int j = 0; j < emptySpots[i].Count; j++)
                    {
                        bool edge = GetNeighbor(grid, emptySpots[i][j].X, emptySpots[i][j].Y, 0, 1) == 1;

                        if (edge)
                        {
                            bottomEdges.Add(emptySpots[i][j]);
                        }
                    }

                    allBottomEdges.Add(bottomEdges);
                }

                Dictionary<Point, float> hitPointDistances = new Dictionary<Point, float>();
                Dictionary<Point, Point> hitPoints = new Dictionary<Point, Point>();

                //Further filters the edges to only get the ones that don't collide with themselves (when aiming downward)
                for (int i = 0; i < allBottomEdges.Count; i++)
                {
                    for (int j = allBottomEdges[i].Count - 1; j >= 0; j--)
                    {
                        List<Point> tested = new List<Point>();
                        bool thisHitsSelf = false;
                        Point original = new Point(allBottomEdges[i][j].X, allBottomEdges[i][j].Y);
                        Point curr = original;
                        while (true)
                        {
                            tested.Add(curr);

                            int below = GetNeighbor(grid, curr.X, curr.Y, 0, 1);
                            if (below == -1)
                            {
                                thisHitsSelf = true;
                                break;
                            }
                            Point belowPoint = new Point(curr.X, curr.Y + 1);
                            if (below == 0)
                            {
                                for (int k = 0; k < emptySpots[i].Count; k++)
                                {
                                    if (belowPoint == emptySpots[i][k])
                                    {
                                        thisHitsSelf = true;
                                        break;
                                    }
                                }

                                hitPointDistances.Add(original, Vector2.Distance(new Vector2(original.X, original.Y), new Vector2(belowPoint.X, belowPoint.Y)));
                                hitPoints.Add(original, belowPoint);
                                break;
                            }

                            if (thisHitsSelf)
                            {
                                break;
                            }

                            curr = belowPoint;
                        }

                        if (thisHitsSelf)
                        {
                            allBottomEdges[i].RemoveAt(j);
                        }

                    }
                }

                List<List<Vector2>> closestOptions = new List<List<Vector2>>();
                int amountOfClosestPointsToGet = 100;
                for (int i = 0; i < allBottomEdges.Count; i++)
                {
                    List<Vector2> closestPoints = new List<Vector2>();
                    for (int k = 0; k < amountOfClosestPointsToGet; k++)
                    {
                        float closestDist = 100000;
                        Point closest = new Point();
                        for (int j = allBottomEdges[i].Count - 1; j >= 0; j--)
                        {
                            float dist = hitPointDistances[allBottomEdges[i][j]];

                            if (dist < closestDist)
                            {
                                closestDist = dist;
                                closest = allBottomEdges[i][j];
                            }
                        }

                        closestPoints.Add(new Vector2(closest.X, closest.Y));
                        allBottomEdges[i].Remove(closest);
                        if (allBottomEdges[i].Count <= 0)
                        {
                            break;
                        }
                    }
                    closestOptions.Add(closestPoints);
                }

                Dictionary<int, List<int>> fromConnections = new Dictionary<int, List<int>>();
                List<List<Point>> linesToCreate = new List<List<Point>>();

                for (int i = 0; i < emptySpots.Count; i++)
                {
                    fromConnections.Add(i, new List<int>());
                }

                for (int i = 0; i < closestOptions.Count; i++)
                {
                    List<Point> line = new List<Point>();

                    int rayLength = 20;

                    List<PixelFlat> flatnessValues = new List<PixelFlat>();
                    for (int j = 0; j < closestOptions[i].Count; j++)
                    {
                        Console.WriteLine("Calculating best split location, checking point " + (j + 1));

                        //Top
                        Point evalPointStart = new Point((int)closestOptions[i][j].X, (int)closestOptions[i][j].Y);
                        Point insideRightStart = PixelRaycast(grid, evalPointStart, new Point(1, 0), 1, rayLength);
                        Point insideLeftStart = PixelRaycast(grid, evalPointStart, new Point(-1, 0), 1, rayLength);
                        Point outsideRightStart = PixelRaycast(grid, new Point(evalPointStart.X, evalPointStart.Y + 1), new Point(1, 0), 0, rayLength);
                        Point outsideLeftStart = PixelRaycast(grid, new Point(evalPointStart.X, evalPointStart.Y + 1), new Point(-1, 0), 0, rayLength);

                        Vector2 outsideMiddleStart = Vector2.Lerp(new Vector2(outsideLeftStart.X, outsideLeftStart.Y), new Vector2(outsideRightStart.X, outsideRightStart.Y), 0.5f);
                        float outsideCheckStart = (rayLength * 0.5f) * Vector2.Distance(outsideMiddleStart, new Vector2(evalPointStart.X, evalPointStart.Y));
                        Vector2 insideMiddleStart = Vector2.Lerp(new Vector2(insideLeftStart.X, insideLeftStart.Y), new Vector2(insideRightStart.X, insideRightStart.Y), 0.5f);
                        float insideCheckStart = (rayLength * 0.5f) * Vector2.Distance(insideMiddleStart, new Vector2(evalPointStart.X, evalPointStart.Y));

                        float outsideLengthStart = Vector2.Distance(new Vector2(outsideLeftStart.X, outsideLeftStart.Y), new Vector2(outsideRightStart.X, outsideRightStart.Y));
                        float insideLengthStart = Vector2.Distance(new Vector2(insideLeftStart.X, insideLeftStart.Y), new Vector2(insideRightStart.X, insideRightStart.Y));
                        float lengthDifferenceStart = (rayLength * 4) - (outsideLengthStart + insideLengthStart);

                        float totalValidAreaStart = outsideCheckStart + insideCheckStart + lengthDifferenceStart;

                        //Bottom
                        Point evalPointEnd = hitPoints[evalPointStart];
                        Point insideRightEnd = PixelRaycast(grid, new Point(evalPointEnd.X, evalPointEnd.Y + 1), new Point(1, 0), 1, rayLength);
                        Point insideLeftEnd = PixelRaycast(grid, new Point(evalPointEnd.X, evalPointEnd.Y + 1), new Point(-1, 0), 1, rayLength);
                        Point outsideRightEnd = PixelRaycast(grid, new Point(evalPointEnd.X, evalPointEnd.Y - 1), new Point(1, 0), 0, rayLength);
                        Point outsideLeftEnd = PixelRaycast(grid, new Point(evalPointEnd.X, evalPointEnd.Y - 1), new Point(-1, 0), 0, rayLength);

                        Vector2 outsideMiddleEnd = Vector2.Lerp(new Vector2(outsideLeftEnd.X, outsideLeftEnd.Y), new Vector2(outsideRightEnd.X, outsideRightEnd.Y), 0.5f);
                        float outsideCheckEnd = (rayLength * 0.5f) * Vector2.Distance(outsideMiddleEnd, new Vector2(evalPointEnd.X, evalPointEnd.Y));
                        Vector2 insideMiddleEnd = Vector2.Lerp(new Vector2(insideLeftEnd.X, insideLeftEnd.Y), new Vector2(insideRightEnd.X, insideRightEnd.Y), 0.5f);
                        float insideCheckEnd = (rayLength * 0.5f) * Vector2.Distance(insideMiddleEnd, new Vector2(evalPointEnd.X, evalPointEnd.Y));

                        float outsideLengthEnd = Vector2.Distance(new Vector2(outsideLeftEnd.X, outsideLeftEnd.Y), new Vector2(outsideRightEnd.X, outsideRightEnd.Y));
                        float insideLengthEnd = Vector2.Distance(new Vector2(insideLeftEnd.X, insideLeftEnd.Y), new Vector2(insideRightEnd.X, insideRightEnd.Y));
                        float lengthDifferenceEnd = (rayLength * 4) - (outsideLengthEnd + insideLengthEnd);

                        float totalValidAreaEnd = outsideCheckEnd + insideCheckEnd + lengthDifferenceEnd;

                        flatnessValues.Add(new PixelFlat()
                        {
                            Point = evalPointStart,
                            FlatnessValue = totalValidAreaStart + totalValidAreaEnd
                        });

                        if(i == -1)
                        {
                            VMFDebug.CreateDebugImage("EvalOption" + j, onDraw: (g) =>
                            {
                                for (int i = 0; i < grid.GetLength(0); i++)
                                {
                                    for (int j = 0; j < grid.GetLength(1); j++)
                                    {
                                        g.DrawRectangle(grid[i, j] == 0 ? Pens.Gray : Pens.White, new Rectangle(i, j, 1, 1));
                                    }
                                }
                                g.DrawLine(Pens.DarkBlue, evalPointStart, insideRightStart);
                                g.DrawLine(Pens.DarkBlue, evalPointStart, insideLeftStart);
                                g.DrawLine(Pens.LightBlue, new Point(evalPointStart.X, evalPointStart.Y + 1), outsideRightStart);
                                g.DrawLine(Pens.LightBlue, new Point(evalPointStart.X, evalPointStart.Y + 1), outsideLeftStart);
                                g.DrawEllipse(Pens.DarkBlue, new Rectangle((int)insideMiddleStart.X, (int)insideMiddleStart.Y, 2, 2));
                                g.DrawEllipse(Pens.LightBlue, new Rectangle((int)outsideMiddleStart.X, (int)outsideMiddleStart.Y, 2, 2));

                                g.DrawLine(Pens.DarkGreen, new Point(evalPointEnd.X, evalPointEnd.Y + 1), insideRightEnd);
                                g.DrawLine(Pens.DarkGreen, new Point(evalPointEnd.X, evalPointEnd.Y + 1), insideLeftEnd);
                                g.DrawLine(Pens.LightGreen, new Point(evalPointEnd.X, evalPointEnd.Y - 1), outsideRightEnd);
                                g.DrawLine(Pens.LightGreen, new Point(evalPointEnd.X, evalPointEnd.Y - 1), outsideLeftEnd);
                                g.DrawEllipse(Pens.DarkGreen, new Rectangle((int)insideMiddleEnd.X, (int)insideMiddleEnd.Y, 2, 2));
                                g.DrawEllipse(Pens.LightGreen, new Rectangle((int)outsideMiddleEnd.X, (int)outsideMiddleEnd.Y, 2, 2));

                                g.DrawLine(Pens.Red, evalPointStart, evalPointEnd);
                        
                                g.DrawRectangle(Pens.Black, new Rectangle((int)evalPointStart.X, (int)evalPointStart.Y, 1, 1));
                                g.DrawRectangle(Pens.Black, new Rectangle((int)evalPointEnd.X, (int)evalPointEnd.Y, 1, 1));

                                g.DrawString("Total: " + (totalValidAreaStart + totalValidAreaEnd) + "\n" +
                                             "Top Middle Check: " + (outsideCheckStart + insideCheckStart) + "\n" +
                                             "Top Length Check: " + (lengthDifferenceStart) + "/" + (rayLength * 4) + "\n" +
                                             "Bottom Middle Check: " + (outsideCheckEnd + insideCheckEnd) + "\n" +
                                             "Bottom Length Check: " + (lengthDifferenceEnd) + "/" + (rayLength * 4), 
                                             SystemFonts.DefaultFont, Brushes.Black, new PointF(0, 0));
                            }, map.Width, map.Height);
                        }
                    }
                    flatnessValues.Sort((x, y) => { return x.FlatnessValue.CompareTo(y.FlatnessValue); });

                    //Not sure if theres a better way to choose out of the top contenders, maybe explore later
                    Point current = flatnessValues[0].Point;
                    Point goal = hitPoints[new Point((int)current.X, (int)current.Y)];

                    int from = -1;
                    int to = -1;
                    for (int j = 0; j < emptySpots.Count; j++)
                    {
                        for (int k = 0; k < emptySpots[j].Count; k++)
                        {
                            if (emptySpots[j][k] == current)
                            {
                                from = j;
                                break;
                            }
                        }
                        if (from != -1)
                        {
                            break;
                        }
                    }
                    for (int j = 0; j < emptySpots.Count; j++)
                    {
                        for (int k = 0; k < emptySpots[j].Count; k++)
                        {
                            if (emptySpots[j][k] == goal)
                            {
                                to = j;
                                break;
                            }
                        }
                        if (to != -1)
                        {
                            break;
                        }
                    }

                    bool loops = false;
                    List<int> visited = new List<int>();
                    Queue<int> toCheck = new Queue<int>();
                    toCheck.Enqueue(from);
                    int check = -1;
                    while (toCheck.Count > 0)
                    {
                        check = toCheck.Dequeue();

                        visited.Add(check);

                        if (check == to)
                        {
                            loops = true;
                            break;
                        }

                        if (fromConnections[check].Count == 0)
                        {
                            continue;
                        }

                        for (int j = 0; j < fromConnections[check].Count; j++)
                        {
                            if (!visited.Contains(fromConnections[check][j]))
                            {
                                toCheck.Enqueue(fromConnections[check][j]);
                            }
                        }
                    }

                    if (!loops)
                    {
                        fromConnections[to].Add(from);

                        while (true)
                        {
                            line.Add(current);

                            if (current == goal)
                            {
                                break;
                            }

                            current = new Point(current.X, current.Y + 1);
                        }

                        linesToCreate.Add(line);
                    }
                }

                Console.WriteLine("Adding loop points");
                for (int i = 0; i < linesToCreate.Count; i++)
                {
                    for (int j = 0; j < linesToCreate[i].Count; j++)
                    {
                        grid[linesToCreate[i][j].X, linesToCreate[i][j].Y] = 0;
                    }
                }


                VMFDebug.CreateDebugImage("LoopPoints", onDraw: (g) =>
                {
                    for (int i = 0; i < grid.GetLength(0); i++)
                    {
                        for (int j = 0; j < grid.GetLength(1); j++)
                        {
                            g.DrawRectangle(grid[i, j] == 0 ? Pens.Gray : Pens.White, new Rectangle(i, j, 1, 1));
                        }
                    }
                    for (int i = 0; i < emptySpots.Count; i++)
                    {
                        g.DrawString(i.ToString(), SystemFonts.DefaultFont, Brushes.Red, new PointF(emptySpots[i][0].X, emptySpots[i][0].Y));
                    }
                    for (int i = 0; i < linesToCreate.Count; i++)
                    {
                        Vector2 pos = Vector2.Lerp(new Vector2(linesToCreate[i][0].X, linesToCreate[i][0].Y), new Vector2(linesToCreate[i][linesToCreate[i].Count - 1].X, linesToCreate[i][linesToCreate[i].Count - 1].Y), 0.5f);
                        g.DrawString(i.ToString(), SystemFonts.DefaultFont, Brushes.Blue, new PointF(pos.X, pos.Y));
                    }
                    for (int i = 0; i < closestOptions.Count; i++)
                    {
                        for (int j = 0; j < closestOptions[i].Count; j++)
                        {
                            g.DrawRectangle(Pens.Black, new Rectangle((int)closestOptions[i][j].X, (int)closestOptions[i][j].Y, 1, 1));
                        }
                    }
                }, map.Width, map.Height);
            }


            List<Vector2> linedUpPoints = new List<Vector2>();

            bool[,] boolGrid = new bool[grid.GetLength(0), grid.GetLength(1)];
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    boolGrid[x, y] = grid[x, y] == 1;
                }
            }

            linedUpPoints = new List<Vector2>(
                BitmapHelper.CreateFromBitmap(new ArrayBitmap(boolGrid))
                );

            List<Vector2> sharedPoints = new List<Vector2>();
            Dictionary<int, Vector2> toMove = new Dictionary<int, Vector2>();

            List<int> badValues = new List<int>();

            bool swapValueToRemove = true;
            for (int i = linedUpPoints.Count - 1; i >= 0; i--)
            {
                for (int j = linedUpPoints.Count - 1; j >= i; j--)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    float dist = Vector2.Distance(linedUpPoints[i], linedUpPoints[j]);

                    if (dist <= 2 && !sharedPoints.Contains(linedUpPoints[i]) && !sharedPoints.Contains(linedUpPoints[j]))
                    {
                        int diff = j - i;
                        if (diff < 4)
                        {
                            if(swapValueToRemove)
                            {
                                badValues.Add(i);
                            }
                            else
                            {
                                badValues.Add(j);
                            }

                            swapValueToRemove = !swapValueToRemove;
                        }
                        else
                        {
                            sharedPoints.Add(linedUpPoints[i]);
                            sharedPoints.Add(linedUpPoints[j]);
                            toMove.Add(i, linedUpPoints[j]);
                        }
                    }
                }
            }

            foreach (int key in toMove.Keys)
            {
                linedUpPoints[key] = toMove[key];
            }
            
            for (int i = linedUpPoints.Count; i >= 0; i--)
            {
                if(badValues.Contains(i))
                {
                    linedUpPoints.RemoveAt(i);
                }
            }

            VMFDebug.CreateDebugImage("ImageProcess", onDraw: (g) =>
            {
                float scale = 0.4f;

                for (int i = 0; i < linedUpPoints.Count; i++)
                {
                    int iN = (i + 1) % (linedUpPoints.Count);
                    g.DrawRectangle(new Pen(Color.Black), new Rectangle((int)(linedUpPoints[i].X * scale), (int)(linedUpPoints[i].Y * scale), 1, 1));
                    g.DrawLine(new Pen(Color.Black, 3), 
                        new Point((int)(linedUpPoints[i].X * scale), (int)(linedUpPoints[i].Y * scale)), 
                        new Point((int)(linedUpPoints[iN].X * scale), (int)(linedUpPoints[iN].Y * scale)));

                    if(linedUpPoints[i] == linedUpPoints[iN])
                    {
                        g.DrawEllipse(new Pen(Color.Blue, 3), new Rectangle((int)(linedUpPoints[i].X * scale), (int)(linedUpPoints[i].Y * scale), 1, 1));
                    }

                    g.DrawString(i.ToString(), SystemFonts.DefaultFont, Brushes.Red, new Point((int)(linedUpPoints[i].X * scale), (int)(linedUpPoints[i].Y * scale)));
                }

                for (int i = 0; i < linedUpPoints.Count; i++)
                {
                    g.DrawRectangle(new Pen(Color.FromArgb(i % 255, i % 255, i % 255)), new Rectangle((int)(linedUpPoints[i].X * scale), (int)(linedUpPoints[i].Y * scale), 1, 1));
                }
            });


            Polygon poly = new Polygon()
            {
                Visgroup = Visgroups.TAR_LAYOUT,
                Position = new Vector3(-map.Width, -map.Height, 0),
                Data = new PolygonShapeData()
                {
                    Depth = 64,
                    Scalar = 2,
                    PolygonPoints = linedUpPoints
                }
            };
            
            shapes.Add(poly);
            
            shapes.AddRange(WallGenerator.CreateWalls(poly, new WallData()
            {
                Height = 256,
                Thickness = 16
            }));

            return shapes;
        }

        public Point PixelRaycast(int[,] grid, Point start, Point direction, int valueToStopOn, int maxDistance = 1000)
        {
            int distanceIncrease = 0;
            Point curr = start;
            while (true)
            {
                int nextPosition = GetNeighbor(grid, curr.X, curr.Y, direction.X, direction.Y);
                if (nextPosition == -1)
                {
                    return curr;
                }
                Point nextPoint = new Point(curr.X + direction.X, curr.Y + direction.Y);
                if (nextPosition == valueToStopOn)
                {
                    return nextPoint;
                }

                curr = nextPoint;
                distanceIncrease++;
                if(distanceIncrease >= maxDistance)
                {
                    return curr;
                }
            }
        }

        public List<Point> GetEmptySpace(ref int[,] grid, int x, int y)
        {
            List<Point> output = new List<Point>();
            int currX = x;
            int currY = y;
            Queue<Point> pointsToCheck = new Queue<Point>();
            Point start = new Point(x, y);
            pointsToCheck.Enqueue(start);

            int target = 0;
            int replaceWith = 1;


            while(pointsToCheck.Count > 0)
            {
                Point currPoint = pointsToCheck.Dequeue();
                currX = currPoint.X;
                currY = currPoint.Y;

                if (grid[currX, currY] != target)
                {
                    continue;
                }
                else
                {
                    grid[currX, currY] = replaceWith;
                    output.Add(new Point(currX, currY));
                }

                List<Point> nextPoints = new List<Point>()
                {
                    new Point(-1, 0),
                    new Point(1, 0),
                    new Point(0, -1),
                    new Point(0, 1)
                };

                for (int i = 0; i < nextPoints.Count; i++)
                {
                    int xi = nextPoints[i].X;
                    int yi = nextPoints[i].Y;

                    if (currX + xi == -1 || currY + yi == -1 || currX + xi == grid.GetLength(0) || currY + yi == grid.GetLength(1))
                    {
                        continue;
                    }

                    Point nextPoint = new Point(currX + xi, currY + yi);

                    pointsToCheck.Enqueue(nextPoint);
                }
            }

            return output;
        }


        private int GetNeighbor(int[,] grid, int pointX, int pointY, int x, int y)
        {
            if (pointX == 0 || pointY == 0 || pointX == grid.GetLength(0) - 1 || pointY == grid.GetLength(1) - 1)
            {
                return -1;
            }

            return grid[pointX + x, pointY + y];
        }
    }

    public class MiscGenerationMethod : GenerationMethod
    {
        public override List<Shape> GetBrushes()
        {
            List<Shape> shapes = new List<Shape>();

            //Manaully made convex shape
            Polygon floor = new Polygon()
            {
                Visgroup = Visgroups.TAR_LAYOUT,
                Position = new Vector3(-256, -256, 16),
                Data = new PolygonShapeData()
                {
                    Depth = 32,
                    Scalar = 128,
                    PolygonPoints = new List<Vector2>()
                    {
                        new Vector2(-3, -1),
                        new Vector2(4, -1),
                        new Vector2(4, 2),
                        new Vector2(3, 3),
                        new Vector2(3, 4),
                        new Vector2(4, 6),
                        new Vector2(7, 6),
                        new Vector2(8, 8),
                        new Vector2(8, 10),
                        new Vector2(6, 10),
                        new Vector2(6, 8),
                        new Vector2(4, 8),
                        new Vector2(4, 15),
                        new Vector2(2, 16),
                        new Vector2(-1, 16),
                        new Vector2(-1, 18),
                        new Vector2(4, 18),
                        new Vector2(6, 16),
                        new Vector2(6, 10),
                        new Vector2(8, 10),
                        new Vector2(8, 12),
                        new Vector2(10, 12),
                        new Vector2(11, 13),
                        new Vector2(11, 15),
                        new Vector2(10, 16),
                        new Vector2(8, 16),
                        new Vector2(6, 18),
                        new Vector2(6, 20),
                        new Vector2(4, 22),
                        new Vector2(0, 22),
                        new Vector2(-2, 20),
                        new Vector2(-6, 20),
                        new Vector2(-8, 18),
                        new Vector2(-8, 16),
                        new Vector2(-6, 14),
                        new Vector2(-4, 14),
                        new Vector2(-4, 12),
                        new Vector2(-6, 10),
                        new Vector2(-6, 6),
                        new Vector2(-3, 6),
                        new Vector2(-3, 4),
                        new Vector2(-1, 4),
                        new Vector2(-2, 10),
                        new Vector2(-2, 14),
                        new Vector2(0, 14),
                        new Vector2(0, 8),
                        new Vector2(1, 8),
                        new Vector2(1, 3),
                        new Vector2(0, 2),
                        new Vector2(0, 1),
                        new Vector2(-1, 1),
                        new Vector2(-1, 4),
                        new Vector2(-3, 4)
                    }
                }
            };
            shapes.Add(floor);


            shapes.AddRange(StairsGenerator.Generate(new StairData()
            {
                Visgroup = Visgroups.TAR_LAYOUT,
                RailingThickness = 8,
                FuncDetailId = EntityTemplates.FuncDetailID++,
                Position = new Vector3(-256, 0, 128),
                Run = 12,
                Rise = 8,
                StairCount = 16,
                StairWidth = 128,
                Direction = Direction.East
            }, 
            new RotationData()
            {
                RotationAxis = new Vector3(0, 0, 1),
                RotationAngle = 0
            }
            ));
            shapes.AddRange(StairsGenerator.Generate(new StairData()
            {
                Visgroup = Visgroups.TAR_LAYOUT,
                RailingThickness = 8,
                FuncDetailId = EntityTemplates.FuncDetailID++,
                Position = new Vector3(256, 0, 128),
                Run = 12,
                Rise = 8,
                StairCount = 16,
                StairWidth = 128,
                Direction = Direction.West
            },
            new RotationData()
            {
                RotationAxis = new Vector3(0, 0, 1),
                RotationAngle = 0
            }
            ));
            shapes.AddRange(StairsGenerator.Generate(new StairData()
            {
                Visgroup = Visgroups.TAR_LAYOUT,
                RailingThickness = 8,
                FuncDetailId = EntityTemplates.FuncDetailID++,
                Position = new Vector3(0, -256, 128),
                Run = 12,
                Rise = 8,
                StairCount = 16,
                StairWidth = 128,
                Direction = Direction.North
            },
            new RotationData()
            {
                RotationAxis = new Vector3(0, 0, 1),
                RotationAngle = 0
            }
            ));
            shapes.AddRange(StairsGenerator.Generate(new StairData()
            {
                Visgroup = Visgroups.TAR_LAYOUT,
                RailingThickness = 8,
                FuncDetailId = EntityTemplates.FuncDetailID++,
                Position = new Vector3(0, 256, 128),
                Run = 12,
                Rise = 8,
                StairCount = 16,
                StairWidth = 128,
                Direction = Direction.South
            },
            new RotationData()
            {
                RotationAxis = new Vector3(0, 0, 1),
                RotationAngle = 0
            }
            ));

            shapes.AddRange(WallGenerator.CreateWalls(floor, new WallData()
            {
                Height = 256,
                Thickness = 64,
                facesIndicesToSkip = new List<int>()
            }));

            return shapes;
        }
    }

    public class HollowCubeGenerationMethod : GenerationMethod
    {
        public Vector3 Position = Vector3.Zero;
        public Vector3 Size;
        public int Scalar = 32;
        public float Thickness;
        public string Texture = Textures.DEV_MEASUREGENERIC01B;
        public override List<Shape> GetBrushes()
        {
            List<Shape> shapes = new List<Shape>();

            //Top
            shapes.Add(new Cube()
            {
                Position = this.Position * Scalar + new Vector3(0, 0, Size.Z) * Scalar,
                Texture = this.Texture,
                Data = new CubeShapeData()
                {
                    Size = new Vector3(Size.X, Size.Y, Thickness) * Scalar * 2
                }
            });

            //Bottom
            shapes.Add(new Cube()
            {
                Position = this.Position * Scalar + new Vector3(0, 0, -Size.Z) * Scalar,
                Texture = this.Texture,
                Data = new CubeShapeData()
                {
                    Size = new Vector3(Size.X, Size.Y, Thickness) * Scalar * 2
                }
            });

            //Front
            shapes.Add(new Cube()
            {
                Position = this.Position * Scalar + new Vector3(0, Size.Y, 0) * Scalar,
                Texture = this.Texture,
                Data = new CubeShapeData()
                {
                    Size = new Vector3(Size.X, Thickness, Size.Z) * Scalar * 2
                }
            });

            //Back
            shapes.Add(new Cube()
            {
                Position = this.Position * Scalar + new Vector3(0, -Size.Y, 0) * Scalar,
                Texture = this.Texture,
                Data = new CubeShapeData()
                {
                    Size = new Vector3(Size.X, Thickness, Size.Z) * Scalar * 2
                }
            });

            //Left
            shapes.Add(new Cube()
            {
                Position = this.Position * Scalar + new Vector3(Size.X, 0, 0) * Scalar,
                Texture = this.Texture,
                Data = new CubeShapeData()
                {
                    Size = new Vector3(Thickness, Size.Y, Size.Z) * Scalar * 2
                }
            });

            //Right
            shapes.Add(new Cube()
            {
                Position = this.Position * Scalar + new Vector3(-Size.X, 0, 0) * Scalar,
                Texture = this.Texture,
                Data = new CubeShapeData()
                {
                    Size = new Vector3(Thickness, Size.Y, Size.Z) * Scalar * 2
                }
            });

            return shapes;
        }
    }

    public abstract class GenerationMethod
    {
        public abstract List<Shape> GetBrushes();
    }
}
