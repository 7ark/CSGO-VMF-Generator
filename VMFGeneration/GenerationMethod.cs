using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Principal;
using VMFGenerator;

namespace VMFGenerator
{
    public class ImageGenerationMethod : GenerationMethod
    {
        private struct PixelFlat
        {
            public Point Point;
            public float FlatnessValue;
        }

        public string InputFilePath;

        public override List<Shape> GetBrushes(out List<string> entities)
        {
            entities = new List<string>();
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
        public override List<Shape> GetBrushes(out List<string> entities)
        {
            entities = new List<string>();
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
                BlockEntityID = EntityTemplates.BlockEntityID++,
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
                BlockEntityID = EntityTemplates.BlockEntityID++,
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
                BlockEntityID = EntityTemplates.BlockEntityID++,
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
                BlockEntityID = EntityTemplates.BlockEntityID++,
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
        public override List<Shape> GetBrushes(out List<string> entities)
        {
            entities = new List<string>();
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

    public class BasicSpawnsGenerationMethod : GenerationMethod
    {
        public override List<Shape> GetBrushes(out List<string> entities)
        {
            entities = new List<string>();
            entities.Add(EntityTemplates.InfoPlayerTerrorist(
                origin: new Vector3(0, -64, 32))
                );
            entities.Add(EntityTemplates.InfoPlayerCounterTerrorist(
                origin: new Vector3(128, -64, 32))
                );

            return new List<Shape>();
        }
    }

    public class SimpleTemplateGenerationMethod : GenerationMethod
    {
        public override List<Shape> GetBrushes(out List<string> entities)
        {
            entities = new List<string>();
            List<Shape> shapes = new List<Shape>();

            shapes.Add(new Polygon()
            {
                Data = new PolygonShapeData()
                {
                    Depth = 32,
                    Scalar = 512,
                    PolygonPoints = new List<Vector2>()
                    {
                        new Vector2(-1, -1),
                        new Vector2(1, -1),
                        new Vector2(1, 1),
                        new Vector2(-1, 1),
                    }
                }
            });

            return shapes;
        }
    }
    public class AimMapGenerationMethod : GenerationMethod
    {
        public int MaxEnemies = 10;
        public bool PlayerIsTerrorist = true;
        public int mapSize = 768;

        public int overrideMinStairsCount = -1;
        public int overrideMaxStairsCount = -1;
        public int overrideMin56BoxCount = -1;
        public int overrideMax56BoxCount = -1;
        public int overrideMin64BoxCount = -1;
        public int overrideMax64BoxCount = -1;
        public override List<Shape> GetBrushes(out List<string> entities)
        {
            entities = new List<string>();
            List<Shape> shapes = new List<Shape>();

            Random random = new Random();

            Polygon floor = new Polygon()
            {
                Texture = Textures.DUST_STONEWALL02,
                Position = new Vector3(0, 0, -16),
                Data = new PolygonShapeData()
                {
                    Depth = 32,
                    Scalar = mapSize,
                    PolygonPoints = new List<Vector2>()
                    {
                        new Vector2(-1, -1),
                        new Vector2(1, -1),
                        new Vector2(1, 1),
                        new Vector2(-1, 1),
                    }
                }
            };
            shapes.Add(floor);

            shapes.Add(new Polygon()
            {
                Position = new Vector3(0, 0, 112),
                Texture = Textures.TRIGGER,
                BlockEntityID = EntityTemplates.BlockEntityID++,
                EntityType = EntityTemplates.BlockEntityType.func_buyzone_all,
                Data = new PolygonShapeData()
                {
                    Depth = 256,
                    Scalar = mapSize,
                    PolygonPoints = new List<Vector2>()
                    {
                        new Vector2(-1, -1),
                        new Vector2(1, -1),
                        new Vector2(1, 1),
                        new Vector2(-1, 1),
                    }
                }
            });

            mapSize -= 32;

            float badDistance = mapSize / 8;
            int amountOfPoints = mapSize / 7;
            List<Vector2> randomPoints = new List<Vector2>();
            for (int i = 0; i < amountOfPoints; i++)
            {
                Vector2 point = new Vector2(random.Next(-mapSize, mapSize + 1), random.Next(-mapSize, mapSize + 1));

                while(true)
                {
                    bool goodToContinue = true;
                    for (int j = 0; j < randomPoints.Count; j++)
                    {
                        float dist = Vector2.Distance(point, randomPoints[j]);
                        if (dist < badDistance)
                        {
                            goodToContinue = false;
                        }
                    }

                    if(goodToContinue)
                    {
                        break;
                    }
                    else
                    {
                        point = new Vector2(random.Next(-mapSize, mapSize + 1), random.Next(-mapSize, mapSize + 1));
                    }
                }
                randomPoints.Add(point);
            }

            Queue<Vector2> pointsToAdd = new Queue<Vector2>(randomPoints);

            int stairsToAdd = random.Next(overrideMinStairsCount == -1 ? 1 : overrideMinStairsCount, overrideMaxStairsCount == -1 ? mapSize / 80 : overrideMaxStairsCount);
            for (int i = 0; i < stairsToAdd; i++)
            {
                Vector2 pos = pointsToAdd.Dequeue();
                shapes.AddRange(StairsGenerator.Generate(new StairData()
                {
                    Texture = Textures.DUST_STONEWALL02,
                    Visgroup = Visgroups.TAR_LAYOUT,
                    RailingThickness = 8,
                    BlockEntityID = EntityTemplates.BlockEntityID++,
                    Position = new Vector3(pos.X, pos.Y, 0),
                    Run = 12,
                    Rise = 8,
                    StairCount = 4 + (4 * random.Next(0,5)),
                    StairWidth = 96 + (32 * random.Next(0, 5)),
                    Direction = (Direction)random.Next(0, 4)
                },
                new RotationData()
                {
                    RotationAxis = new Vector3(0, 0, 1),
                    RotationAngle = 0
                }
                ));
            }

            int coverToAdd56 = random.Next(overrideMin56BoxCount == -1 ? mapSize / 70 : overrideMin56BoxCount, overrideMax56BoxCount == -1 ? mapSize / 40 : overrideMax56BoxCount);
            for (int i = 0; i < coverToAdd56; i++)
            {
                Vector2 pos = pointsToAdd.Dequeue();
                entities.Add(EntityTemplates.PropStatic(Models.DustCrateStyle264x64x64, origin: new Vector3(pos.X, pos.Y, 0)));
            }

            int coverToAdd64 = random.Next(overrideMin64BoxCount == -1 ? mapSize / 60 : overrideMin64BoxCount, overrideMax64BoxCount == -1 ? mapSize / 30 : overrideMax64BoxCount);
            for (int i = 0; i < coverToAdd64; i++)
            {
                Vector2 pos = pointsToAdd.Dequeue();
                entities.Add(EntityTemplates.PropStatic(Models.DustCrateStyle264x64x64, origin: new Vector3(pos.X, pos.Y, 0)));
            }

            Vector2 playerPoint = pointsToAdd.Dequeue();
            if (PlayerIsTerrorist)
            {
                entities.Add(EntityTemplates.InfoPlayerTerrorist(
                    origin: new Vector3(playerPoint.X, playerPoint.Y, 4))
                    );
            }
            else
            {
                entities.Add(EntityTemplates.InfoPlayerCounterTerrorist(
                    origin: new Vector3(playerPoint.X, playerPoint.Y, 4))
                    );
            }

            for (int i = 0; i < pointsToAdd.Count && i < MaxEnemies; i++)
            {
                Vector2 botPoint = pointsToAdd.Dequeue();
                if(PlayerIsTerrorist)
                {
                    entities.Add(EntityTemplates.InfoPlayerCounterTerrorist(
                        origin: new Vector3(botPoint.X, botPoint.Y, 4))
                        );
                }
                else
                {
                    entities.Add(EntityTemplates.InfoPlayerTerrorist(
                        origin: new Vector3(botPoint.X, botPoint.Y, 4))
                        );
                }
            }


            shapes.AddRange(WallGenerator.CreateWalls(floor, new WallData()
            {
                Texture = Textures.DUST_STONEWALL02,
                Height = 256,
                Thickness = 64,
                facesIndicesToSkip = new List<int>()
            }));

            return shapes;
        }
    }


    public class BhopGenerationMethod : GenerationMethod
    {
        public override List<Shape> GetBrushes(out List<string> entities)
        {
            entities = new List<string>();
            List<Shape> shapes = new List<Shape>();

            int distanceBetween = 384;
            int blockSize = 64;
            int amountOfBlocks = 75;

            int rotation = 0;
            int rotationChange = 15;

            int pathWidth = 256;

            Random random = new Random();

            List<Vector3> positions = new List<Vector3>();

            for (int i = 0; i < amountOfBlocks + 2; i++)
            {
                int dist = blockSize + distanceBetween;

                Vector3 finalPos = new Vector3(0, 0, 0);
                int safety = 0;

                int currRot = rotation;

                while(true)
                {
                    rotation = currRot;
                    rotation += rotationChange;
                    int randomXDegree = rotation;
                    int randomYDegree = rotation;
                    if (random.Next(0, 7) == 0 || safety > 1000)
                    {
                        rotationChange *= -1;
                    }
                    if (random.Next(0, 2) == 0)
                    {
                        int changeAdjust = random.Next(-5, 6);
                        rotationChange += changeAdjust;
                        rotationChange = Math.Clamp(rotationChange, safety > 5000 ? -40 : -25, safety > 1000 ? 40 : 25);

                        if (rotationChange >= 0)
                        {
                            if (rotationChange < 10)
                            {
                                rotationChange = 10;
                            }
                        }
                        else
                        {
                            if (rotationChange > -10)
                            {
                                rotationChange = -10;
                            }
                        }
                    }

                    float x = (int)((180 / Math.PI) * Math.Cos(Math.PI * randomXDegree / 180.0));
                    float y = (int)((180 / Math.PI) * Math.Sin(Math.PI * randomYDegree / 180.0));
                    x /= 100;
                    y /= 100;
                    x *= dist;
                    y *= dist;

                    finalPos = new Vector3(0, 0, 0);
                    if (i == 1)
                    {
                        finalPos += new Vector3(x, y, 0);
                    }
                    else if (i != 0)
                    {
                        finalPos += shapes[shapes.Count - 1].Position + new Vector3(x, y, 0);
                    }

                    bool goodPath = true;
                    for (int j = 0; j < positions.Count - 4; j++)
                    {
                        if (Vector3.Distance(positions[j], finalPos) < pathWidth * (safety > 5000 ? 2.5f : 4f))
                        {
                            goodPath = false;
                            break;
                        }
                    }

                    if(goodPath)
                    {
                        break;
                    }
                    else
                    {
                        safety++;
                        if(safety > 10000)
                        {
                            break;
                        }
                    }
                }

                positions.Add(finalPos + new Vector3(0, 0, -blockSize * 2));

                if(i != 0 && i != amountOfBlocks + 1)
                {
                    shapes.Add(new Cube()
                    {
                        BlockEntityID = EntityTemplates.BlockEntityID++,
                        EntityType = EntityTemplates.BlockEntityType.func_detail,
                        Position = finalPos,
                        Texture = Textures.DEV_MEASUREGENERIC01B,
                        Data = new CubeShapeData()
                        {
                            Size = new Vector3(blockSize, blockSize, blockSize * 6)
                        }
                    });
                }
            }

            List<Vector2> points = new List<Vector2>();

            for (int k = 0; k < 2; k++)
            {
                for (int i = k == 0 ? 0 : 1; i < positions.Count; i++)
                {
                    //Okay basically all the shit I'm doing here is getting the vertex normal of the current point
                    //and basing how the wall should be made based off that, so all the walls end up as trapazoids.
                    //The only exception is if a wall piece is missing, I then get the intersection based on which
                    //side of the wall it is, and try to flatten it. It's not perfect but it works?
                    int index1 = (i - 1) % positions.Count;
                    if (index1 == -1)
                    {
                        index1 = positions.Count - 1;
                    }
                    int index2 = i % positions.Count;
                    int index3 = (i + 1) % positions.Count;
                    int index4 = (i + 2) % positions.Count;
                    Vector2 a = new Vector2(positions[index1].X, positions[index1].Y);
                    Vector2 b = new Vector2(positions[index2].X, positions[index2].Y);
                    Vector2 c = new Vector2(positions[index3].X, positions[index3].Y);
                    Vector2 d = new Vector2(positions[index4].X, positions[index4].Y);

                    Vector2 abNormal = Vector2.Normalize(Shape.GetNormal2D(a, b));
                    Vector2 bcNormal = Vector2.Normalize(Shape.GetNormal2D(b, c));
                    Vector2 vertexNormalabc = Vector2.Normalize((abNormal + bcNormal)) * pathWidth;
                    Vector2 cdNormal = Vector2.Normalize(Shape.GetNormal2D(c, d));
                    Vector2 vertexNormalbcd = Vector2.Normalize((bcNormal + cdNormal)) * pathWidth;

                    Vector2 point = new Vector2(positions[index2].X, positions[index2].Y) + vertexNormalabc;
                    Vector2 point2 = new Vector2(positions[index3].X, positions[index3].Y) + vertexNormalbcd;
                    if (!points.Contains(point))
                        points.Add(point);
                    //if (!points.Contains(point2))
                    //    points.Add(point2);
                    //points.Add(c);
                    //points.Add(b);
                }

                positions.Reverse();
            }

            shapes.Add(new Polygon()
            {
                Texture = Textures.SKYBOX,
                Position = new Vector3(0, 0, 512),
                Data = new PolygonShapeData()
                {
                    Depth = 32,
                    Scalar = 1,
                    PolygonPoints = new List<Vector2>(points)
                }
            });
            Polygon floor = new Polygon()
            {
                Texture = Textures.DUST_CINDERBLOCK_CHECKERED01,
                Position = new Vector3(0, 0, -96),
                Data = new PolygonShapeData()
                {
                    Depth = 32,
                    Scalar = 1,
                    PolygonPoints = new List<Vector2>(points)
                }
            };
            shapes.Add(floor);
            shapes.Add(new Polygon()
            {
                Texture = Textures.TRIGGER,
                BlockEntityID = EntityTemplates.BlockEntityID++,
                EntityType = EntityTemplates.BlockEntityType.trigger_hurt,
                EntitySettings = new List<string>()
                {
                    "damage", "200",
                    "damagecap", "200",
                    "damagemodel", "0",
                    "damagetype", "0",
                    "nodmgforce", "0",
                    "origin", "",
                    "spawnflags", "4097",
                    "StartDisabled", "0"
                },
                Position = new Vector3(0, 0, -96),
                Data = new PolygonShapeData()
                {
                    Depth = 64,
                    Scalar = 1,
                    PolygonPoints = new List<Vector2>(points)
                }
            });

            shapes.AddRange(WallGenerator.CreateWalls(floor, new WallData()
            {
                Texture = Textures.DEV_MEASUREGENERIC01B,
                Height = 1024,
                Thickness = 64,
                facesIndicesToSkip = new List<int>()
            }));

            entities.Add(EntityTemplates.InfoPlayerTerrorist(
                origin: new Vector3(positions[1].X, positions[1].Y, blockSize * 3 + 4))
                );

            return shapes;
        }
    }

    public abstract class GenerationMethod
    {
        public abstract List<Shape> GetBrushes(out List<string> entities);
    }
}
