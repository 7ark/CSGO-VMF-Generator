using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;

namespace VMFConverter
{
    public class ImageGenerationMethod : GenerationMethod
    {
        public string InputFilePath;
        public int Detail = 10;
        public override List<Shape> GetBrushes()
        {
            List<Shape> shapes = new List<Shape>();

            List<Vector2> edgePositions = new List<Vector2>();
            Bitmap map = new Bitmap(InputFilePath);
            int[,] grid = new int[map.Width, map.Height];

            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    grid[x, y] = map.GetPixel(x, y).G > 20 ? 1 : 0;
                }
            }

            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    bool edge = false;
                    if(grid[x, y] == 1)
                    {
                        for (int localX = -1; localX < 2; localX++)
                        {
                            for (int localY = -1; localY < 2; localY++)
                            {
                                edge = GetNeighbor(grid, x, y, localX, localY) == 0;
                                if(edge)
                                {
                                    break;
                                }
                            }
                            if (edge)
                            {
                                break;
                            }
                        }
                    }
                    if(edge)
                    {
                        edgePositions.Add(new Vector2(x, grid.GetLength(1) - y));
                    }
                }
            }

            List<Vector2> linedUpPoints = new List<Vector2>();

            Vector2 current = edgePositions[0];
            linedUpPoints.Add(current);
            edgePositions.RemoveAt(0);

            while (edgePositions.Count > 0)
            {
                Vector2 closest1 = new Vector2();
                float closestDist = float.MaxValue;
                for (int i = 0; i < edgePositions.Count; i++)
                {
                    float dist = Vector2.Distance(current, edgePositions[i]);
                    if(dist < closestDist)
                    {
                        closest1 = edgePositions[i];
                        closestDist = dist;
                    }
                }

                current = closest1;
                linedUpPoints.Add(current);
                edgePositions.Remove(current);
            }

            List<Vector2> results = new List<Vector2>();
            //Detail degretation
            for (int i = 0; i < linedUpPoints.Count; i += Detail)
            {
                results.Add(linedUpPoints[i]);// - new Vector2(grid.GetLength(0)/2, grid.GetLength(1)/2));
            }

            float scale = 0.5f;
            Pen greyPen = new Pen(Color.Gray, 3);
            Pen blackPen = new Pen(Color.Black, 3);
            Pen redPen = new Pen(Color.Red, 3);
            Pen bluePen = new Pen(Color.Blue, 3);
            Pen greenPen = new Pen(Color.Green, 3);
            using (Bitmap canvas = new Bitmap(500, 500))
            {
                using (Graphics g = Graphics.FromImage(canvas))
                {
                    g.FillRectangle(Brushes.Black, new Rectangle(0, 0, 500, 500));

                    //for (int i = 0; i < results.Count; i++)
                    //{
                    //    Point A = new Point((int)results[i].X, (int)results[i].Y);
                    //    Point B = new Point((int)results[(i + 1) % results.Count].X, (int)results[(i + 1) % results.Count].Y);
                    //    g.DrawLine(new Pen(Color.FromArgb((i % 205) + 50, (i % 205) + 50, (i % 205) + 50)), A, B);
                    //}

                    for (int i = 0; i < linedUpPoints.Count; i++)
                    {
                        g.DrawRectangle(new Pen(Color.FromArgb(i % 255, i % 255, i % 255)), new Rectangle((int)(linedUpPoints[i].X * scale), (int)(linedUpPoints[i].Y * scale), 1, 1));
                    }

                    for (int x = 0; x < grid.GetLength(0); x++)
                    {
                        for (int y = 0; y < grid.GetLength(1); y++)
                        {
                            if(grid[x, y] == 1)
                            g.DrawRectangle(new Pen(Color.White), new Rectangle((int)(x * scale), (int)(y * scale), 1, 1));
                        }
                    }
                }

                canvas.Save(@"C:\Users\funny\source\repos\VMFGenerator\ImageProcess" + ".png");
            }

            Polygon poly = new Polygon()
            {
                Position = new Vector3(0, 0, 0),
                Data = new PolygonShapeData()
                {
                    Depth = 64,
                    Scalar = 8,
                    PolygonPoints = results
                }
            };

            for (int i = 0; i < 20; i++)
            {
                poly = Generator.RemoveRedundantPoints(poly);
            }

            shapes.Add(poly);

            return shapes;
        }

        private int GetNeighbor(int[,] grid, int pointX, int pointY, int x, int y)
        {
            if(pointX == 0 || pointY == 0 || pointX == grid.GetLength(0) - 1 || pointY == grid.GetLength(1) - 1)
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

            //shapes.Add(new Polygon()
            //{
            //    Position = new Vector3(0, 0, -16),
            //    Data = new PolygonShapeData()
            //    {
            //        Depth = 32,
            //        Scalar = 256,
            //        PolygonPoints = new List<Vector2>()
            //        {
            //            new Vector2(-2, -2),
            //            new Vector2(2, -2),
            //            new Vector2(2, 2),
            //            new Vector2(-2, 2),
            //        }
            //    }
            //});

            //Polygon floor = new Polygon()
            //{
            //    Position = new Vector3(-256, -256, 16),
            //    Data = new PolygonShapeData()
            //    {
            //        Depth = 32,
            //        Scalar = 128,
            //        PolygonPoints = new List<Vector2>()
            //        {
            //            new Vector2(0, 0),
            //            new Vector2(5, 0),
            //            new Vector2(6, 2),
            //            new Vector2(6, 5),
            //            new Vector2(4, 6),
            //            new Vector2(1, 6),
            //            new Vector2(0, 4),
            //            new Vector2(0, 2),
            //            new Vector2(1, 2),
            //            new Vector2(1, 4),
            //            new Vector2(4, 4),
            //            new Vector2(4, 2),
            //            new Vector2(3, 1),
            //            new Vector2(0, 1)
            //        }
            //    }
            //};

            //Polygon floor = new Polygon()
            //{
            //    Position = new Vector3(-256, -256, 16),
            //    Data = new PolygonShapeData()
            //    {
            //        Depth = 32,
            //        Scalar = 128,
            //        PolygonPoints = new List<Vector2>()
            //        {
            //            new Vector2(0, 0),
            //            new Vector2(3, 0),
            //            new Vector2(6, 2),
            //            new Vector2(6, 4),
            //            new Vector2(8, 4),
            //            new Vector2(9, 5),
            //            new Vector2(9, 7),
            //            new Vector2(8, 7),
            //            new Vector2(8, 6),
            //            new Vector2(7, 5),
            //            new Vector2(6, 5),
            //            new Vector2(6, 6),
            //            new Vector2(5, 7),
            //            new Vector2(3, 7),
            //            new Vector2(3, 8),
            //            new Vector2(2, 9),
            //            new Vector2(1, 9),
            //            new Vector2(0, 8),
            //            new Vector2(0, 7),
            //            new Vector2(1, 6),
            //            new Vector2(3, 6),
            //            new Vector2(3, 4.5f),
            //            new Vector2(2, 5),
            //            new Vector2(0, 5),
            //            new Vector2(0, 2),//
            //            new Vector2(1, 2),//
            //            new Vector2(1, 4),
            //            new Vector2(2, 4),
            //            new Vector2(3, 3),
            //            new Vector2(3, 2),
            //            new Vector2(2, 1),
            //            new Vector2(1, 1),
            //            new Vector2(1, 2),
            //            new Vector2(0, 2),
            //        }
            //    }
            //};

            Polygon floor = new Polygon()
            {
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

            floor = new Polygon()
            {
                Position = new Vector3(-256, -256, 16),
                Data = new PolygonShapeData()
                {
                    Depth = 32,
                    Scalar = 128,
                    PolygonPoints = new List<Vector2>()
                    {
                        new Vector2(-5, -5),
                        new Vector2(0, -5),
                        new Vector2(0, -10),
                        new Vector2(5, -10),
                        new Vector2(5, 10),
                        new Vector2(-5, 10)
                    }
                }
            };

            shapes.Add(floor);
            //shapes.Add(new Polygon()
            //{
            //    Position = new Vector3(-256, -256, 16),
            //    Data = new PolygonShapeData()
            //    {
            //        Depth = 32,
            //        Scalar = 128,
            //        PolygonPoints = new List<Vector2>()
            //        {
            //            new Vector2(0, 2),
            //            new Vector2(1, 2),
            //            new Vector2(1, 3),
            //            new Vector2(0, 3),
            //        }
            //    }
            //});
            //
            //shapes.Add(new Polygon()
            //{
            //    Position = new Vector3(0, 0, 16),
            //    Data = new PolygonShapeData()
            //    {
            //        Depth = 32,
            //        Scalar = 16,
            //        PolygonPoints = new List<Vector2>()
            //        {
            //            new Vector2(0, 1),
            //            new Vector2(1, 1),
            //            new Vector2(1, 2),
            //            new Vector2(0, 2)
            //        }
            //    }
            //});



            //StairsGenerator stairsGenerator = new StairsGenerator();
            //shapes.AddRange(stairsGenerator.Generate(new StairData()
            //{
            //    RailingThickness = 8,
            //    FuncDetailId = EntityTemplates.FuncDetailID++,
            //    Position = new Vector3(-256, 0, 0),
            //    Run = 12,
            //    Rise = 8,
            //    StairCount = 16,
            //    StairWidth = 128,
            //    Direction = Direction.East
            //}, 
            //new RotationData()
            //{
            //    RotationAxis = new Vector3(0, 0, 1),
            //    RotationAngle = 0
            //}
            //));
            //shapes.AddRange(stairsGenerator.Generate(new StairData()
            //{
            //    RailingThickness = 8,
            //    FuncDetailId = EntityTemplates.FuncDetailID++,
            //    Position = new Vector3(256, 0, 0),
            //    Run = 12,
            //    Rise = 8,
            //    StairCount = 16,
            //    StairWidth = 128,
            //    Direction = Direction.West
            //},
            //new RotationData()
            //{
            //    RotationAxis = new Vector3(0, 0, 1),
            //    RotationAngle = 0
            //}
            //));
            //shapes.AddRange(stairsGenerator.Generate(new StairData()
            //{
            //    RailingThickness = 8,
            //    FuncDetailId = EntityTemplates.FuncDetailID++,
            //    Position = new Vector3(0, -256, 0),
            //    Run = 12,
            //    Rise = 8,
            //    StairCount = 16,
            //    StairWidth = 128,
            //    Direction = Direction.North
            //},
            //new RotationData()
            //{
            //    RotationAxis = new Vector3(0, 0, 1),
            //    RotationAngle = 0
            //}
            //));
            //shapes.AddRange(StairsGenerator.Generate(new StairData()
            //{
            //    RailingThickness = 8,
            //    FuncDetailId = EntityTemplates.FuncDetailID++,
            //    Position = new Vector3(0, 256, 0),
            //    Run = 12,
            //    Rise = 8,
            //    StairCount = 16,
            //    StairWidth = 128,
            //    Direction = Direction.South
            //},
            //new RotationData()
            //{
            //    RotationAxis = new Vector3(0, 0, 1),
            //    RotationAngle = 0
            //}
            //));


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
                    Size = new Vector3(Size.X, Thickness, Size.Y) * Scalar * 2
                }
            });

            //Back
            shapes.Add(new Cube()
            {
                Position = this.Position * Scalar + new Vector3(0, -Size.Y, 0) * Scalar,
                Texture = this.Texture,
                Data = new CubeShapeData()
                {
                    Size = new Vector3(Size.X, Thickness, Size.Y) * Scalar * 2
                }
            });

            //Left
            shapes.Add(new Cube()
            {
                Position = this.Position * Scalar + new Vector3(Size.X, 0, 0) * Scalar,
                Texture = this.Texture,
                Data = new CubeShapeData()
                {
                    Size = new Vector3(Thickness, Size.X, Size.Y) * Scalar * 2
                }
            });

            //Right
            shapes.Add(new Cube()
            {
                Position = this.Position * Scalar + new Vector3(-Size.X, 0, 0) * Scalar,
                Texture = this.Texture,
                Data = new CubeShapeData()
                {
                    Size = new Vector3(Thickness, Size.X, Size.Y) * Scalar * 2
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
