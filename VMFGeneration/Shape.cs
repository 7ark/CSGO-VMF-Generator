using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace VMFConverter
{
    #region Shapes
    public enum Direction { North, South, West, East }
    public struct SolidSide
    {
        public int ID;
        public Vector3[] Plane;
        public Vector4[] UV;
    }

    public class PolygonShapeData : ShapeData
    {
        public int Depth;
        public int Scalar = 1;
        public List<Vector2> PolygonPoints;

        public PolygonShapeData() { }

        public PolygonShapeData(PolygonShapeData self)
        {
            Depth = self.Depth;
            Scalar = self.Scalar;
            PolygonPoints = new List<Vector2>();
            for (int i = 0; i < self.PolygonPoints.Count; i++)
            {
                PolygonPoints.Add(self.PolygonPoints[i]);
            }
        }
    }

    public class Polygon : Shape
    {
        public Polygon() { }
        public Polygon(Polygon self)
        {
            ID = self.ID;
            Position = self.Position;
            if (self.Sides != null)
            {
                Sides = new SolidSide[self.Sides.Length];
            }
            if (Sides != null)
            {
                for (int i = 0; i < self.Sides.Length; i++)
                {
                    Sides[i] = self.Sides[i];
                }
            }
            Data = new PolygonShapeData(self.Data as PolygonShapeData);
        }

        public Polygon Combine(Polygon other)
        {
            PolygonShapeData myShapeData = Data as PolygonShapeData;
            PolygonShapeData shapeData = other.Data as PolygonShapeData;

            List<Vector2> sharedPoints = new List<Vector2>();
            int lowestIndexMe = -1;
            int lowestIndexOther = -1;
            Vector2 lowestMe = new Vector2(99999999, 99999999);
            Vector2 lowestOther = new Vector2(99999999, 99999999);
            Vector2 lowest = new Vector2(99999999, 99999999);

            for (int i = 0; i < myShapeData.PolygonPoints.Count; i++)
            {
                if (shapeData.PolygonPoints.Contains(myShapeData.PolygonPoints[i]))
                {
                    sharedPoints.Add(myShapeData.PolygonPoints[i]);
                }

                if (myShapeData.PolygonPoints[i].X < lowest.X || (myShapeData.PolygonPoints[i].X == lowest.X && myShapeData.PolygonPoints[i].Y < lowest.Y))
                {
                    lowest = myShapeData.PolygonPoints[i];
                }

                if (myShapeData.PolygonPoints[i].X < lowestMe.X || (myShapeData.PolygonPoints[i].X == lowestMe.X && myShapeData.PolygonPoints[i].Y < lowestMe.Y))
                {
                    lowestMe = myShapeData.PolygonPoints[i];
                    lowestIndexMe = i;
                }
            }

            if (sharedPoints.Count <= 1)
            {
                return null;
            }

            for (int i = 0; i < shapeData.PolygonPoints.Count; i++)
            {
                if (shapeData.PolygonPoints[i].X < lowest.X || (shapeData.PolygonPoints[i].X == lowest.X && shapeData.PolygonPoints[i].Y < lowest.Y))
                {
                    lowest = shapeData.PolygonPoints[i];
                }

                if (shapeData.PolygonPoints[i].X < lowestOther.X || (shapeData.PolygonPoints[i].X == lowestOther.X && shapeData.PolygonPoints[i].Y < lowestOther.Y))
                {
                    lowestOther = shapeData.PolygonPoints[i];
                    lowestIndexOther = i;
                }
            }

            bool startOnMe = false;
            int currIndex = -1;
            if (lowestMe != lowestOther)
            {
                if (lowestMe.X == lowestOther.X)
                {
                    startOnMe = lowestMe.Y < lowestOther.Y;
                }
                else
                {
                    startOnMe = lowestMe.X < lowestOther.X;
                }
            }
            else
            {
                int lowestMeNext = lowestIndexMe + 1;
                if (lowestMeNext >= myShapeData.PolygonPoints.Count)
                {
                    lowestMeNext = 0;
                }
                int lowestOtherNext = lowestIndexOther + 1;
                if (lowestOtherNext >= shapeData.PolygonPoints.Count)
                {
                    lowestOtherNext = 0;
                }
                Vector2 mePointOption = myShapeData.PolygonPoints[lowestMeNext];
                Vector2 otherPointOption = shapeData.PolygonPoints[lowestOtherNext];

                float constantAngle = MathF.Atan2(0.5f, 1) * 180 / MathF.PI;
                Vector2 meDifference = Vector2.Normalize(mePointOption - lowest);
                float meAngle = MathF.Atan2(meDifference.X, meDifference.Y) * 180 / MathF.PI;
                Vector2 otherDifference = Vector2.Normalize(otherPointOption - lowest);
                float otherAngle = MathF.Atan2(otherDifference.X, otherDifference.Y) * 180 / MathF.PI;

                startOnMe = meAngle > otherAngle;
            }

            currIndex = startOnMe ? lowestIndexMe : lowestIndexOther;

            List<Vector2> startingList = startOnMe ? myShapeData.PolygonPoints : shapeData.PolygonPoints;
            Vector2 firstPosition = startingList[currIndex];
            List<Vector2> newShapeList = new List<Vector2>();

            int safety = 0;
            while (true)
            {
                Vector2 pos = startingList[currIndex];

                if (pos == firstPosition && newShapeList.Count > 0)
                {
                    break;
                }

                newShapeList.Add(pos);

                if (sharedPoints.Contains(pos) && newShapeList.Count > 1)
                {
                    startingList = startOnMe ? shapeData.PolygonPoints : myShapeData.PolygonPoints;

                    for (int i = 0; i < startingList.Count; i++)
                    {
                        if (startingList[i] == pos)
                        {
                            currIndex = i;
                            break;
                        }
                    }

                    startOnMe = !startOnMe;
                }

                currIndex++;
                if (currIndex >= startingList.Count)
                {
                    currIndex = 0;
                }

                safety++;
                if (safety > 1000)
                {
                    break;
                }
            }

            PolygonShapeData finalData = new PolygonShapeData(myShapeData);

            finalData.PolygonPoints = newShapeList;

            Polygon topVertex = new Polygon()
            {
                ID = other.ID,
                Position = other.Position,
                Sides = other.Sides,
                Data = finalData
            };

            return topVertex;
        }

        public Vector2 GetCenter()
        {
            PolygonShapeData shapeData = Data as PolygonShapeData;
            Vector2 center = new Vector2(0, 0);
            for (int i = 0; i < shapeData.PolygonPoints.Count; i++)
            {
                center += shapeData.PolygonPoints[i] * shapeData.Scalar;
            }

            center /= shapeData.PolygonPoints.Count;

            return center;
        }

        public override void GenerateSides(int startingID)
        {
            PolygonShapeData shapeData = Data as PolygonShapeData;

            //For now we only do cubes 
            Sides = new SolidSide[shapeData.PolygonPoints.Count + 2];

            for (int i = 0; i < Sides.Length; i++)
            {
                Sides[i].ID = startingID + i;
            }

            //Top
            Sides[0].Plane = new Vector3[]
            {
                    Position + new Vector3(-shapeData.Scalar, -shapeData.Scalar, (shapeData.Depth * 0.5f)),
                    Position + new Vector3(-shapeData.Scalar,  shapeData.Scalar, (shapeData.Depth * 0.5f)),
                    Position + new Vector3( shapeData.Scalar,  shapeData.Scalar, (shapeData.Depth * 0.5f))
            };
            Sides[0].UV = new Vector4[]
            {
                    new Vector4(1, 0, 0, 0),
                    new Vector4(0, 1, 0, 0)
            };

            //Bottom
            Sides[1].Plane = new Vector3[]
            {
                    Position + new Vector3( shapeData.Scalar, -shapeData.Scalar, -(shapeData.Depth * 0.5f)),
                    Position + new Vector3( shapeData.Scalar,  shapeData.Scalar, -(shapeData.Depth * 0.5f)),
                    Position + new Vector3(-shapeData.Scalar,  shapeData.Scalar, -(shapeData.Depth * 0.5f))
            };
            Sides[1].UV = new Vector4[]
            {
                    new Vector4(1, 0, 0, 0),
                    new Vector4(0, 1, 0, 0)
            };

            for (int i = 0; i < shapeData.PolygonPoints.Count; i++)
            {
                int sidesIndex = 2 + i;
                Vector2 first = shapeData.PolygonPoints[i];
                int sI = i == shapeData.PolygonPoints.Count - 1 ? 0 : i + 1;
                Vector2 second = shapeData.PolygonPoints[sI];

                float angle = MathF.Atan2(second.Y - first.Y, second.X - first.X) * (180 / MathF.PI);
                if (angle < 0)
                {
                    angle += 360;
                }

                //Check for concave

                Sides[sidesIndex].Plane = new Vector3[]
                {
                        Position + new Vector3(first.X, first.Y, -(shapeData.Depth * 0.5f)),
                        Position + new Vector3(first.X, first.Y, (shapeData.Depth * 0.5f)),
                        Position + new Vector3(second.X, second.Y, (shapeData.Depth * 0.5f))
                };

                //bool reverse = angle % 90 != 0;
                bool useFirstMethod = (angle > 0 && angle <= 90) || (angle > 180 && angle <= 270);
                if (useFirstMethod)
                {
                    Sides[sidesIndex].UV = new Vector4[]
                    {
                            new Vector4(0, 1, 0, 0),
                            new Vector4(0, 0, -1, 0)
                    };
                }
                else
                {
                    Sides[sidesIndex].UV = new Vector4[]
                    {
                            new Vector4(1, 0, 0, 0),
                            new Vector4(0, 0, -1, 0)
                    };

                }
            }

            base.GenerateSides(startingID);
        }

        public void CalculatePreGenerateData()
        {
            PolygonShapeData shapeData = Data as PolygonShapeData;

            for (int i = 0; i < shapeData.PolygonPoints.Count; i++)
            {
                int pointX = (int)MathF.Round(shapeData.PolygonPoints[i].X * shapeData.Scalar, 0, MidpointRounding.ToEven);
                int pointY = (int)MathF.Round(shapeData.PolygonPoints[i].Y * shapeData.Scalar, 0, MidpointRounding.ToEven);

                shapeData.PolygonPoints[i] = new Vector2(pointX, pointY);
            }
        }
    }


    public class CubeShapeData : ShapeData
    {
        public Vector3 Size;

        public CubeShapeData() { }
        public CubeShapeData(CubeShapeData self) : base(self)
        {
            Size = self.Size;
        }
    }

    public class Cube : Shape
    {
        public Cube() { }
        public Cube(Cube self) : base(self)
        {
            Data = new CubeShapeData((CubeShapeData)self.Data);
        }
        public override void GenerateSides(int startingID)
        {
            CubeShapeData shapeData = Data as CubeShapeData;

            //For now we only do cubes 
            Sides = new SolidSide[6];

            for (int i = 0; i < Sides.Length; i++)
            {
                Sides[i].ID = startingID + i;
            }

            //Top
            Sides[0].Plane = new Vector3[]
            {
                    Position + new Vector3(-(shapeData.Size.X * 0.5f), -(shapeData.Size.Y * 0.5f), shapeData.Size.Z * 0.5f),
                    Position + new Vector3(-(shapeData.Size.X * 0.5f), (shapeData.Size.Y * 0.5f), shapeData.Size.Z * 0.5f),
                    Position + new Vector3((shapeData.Size.X * 0.5f), (shapeData.Size.Y * 0.5f), shapeData.Size.Z * 0.5f)
            };
            Sides[0].UV = new Vector4[]
            {
                    new Vector4(1, 0, 0, 0),
                    new Vector4(0, 1, 0, 0)
            };

            //Bottom
            Sides[1].Plane = new Vector3[]
            {
                    Position + new Vector3((shapeData.Size.X * 0.5f), -(shapeData.Size.Y * 0.5f), -shapeData.Size.Z * 0.5f),
                    Position + new Vector3((shapeData.Size.X * 0.5f), (shapeData.Size.Y * 0.5f), -shapeData.Size.Z * 0.5f),
                    Position + new Vector3(-(shapeData.Size.X * 0.5f), (shapeData.Size.Y * 0.5f), -shapeData.Size.Z * 0.5f)
            };
            Sides[1].UV = new Vector4[]
            {
                    new Vector4(1, 0, 0, 0),
                    new Vector4(0, 1, 0, 0)
            };

            //Front
            Sides[2].Plane = new Vector3[]
            {
                    Position + new Vector3(-(shapeData.Size.X * 0.5f), (shapeData.Size.Y * 0.5f), shapeData.Size.Z * 0.5f),
                    Position + new Vector3(-(shapeData.Size.X * 0.5f), (shapeData.Size.Y * 0.5f), -shapeData.Size.Z * 0.5f),
                    Position + new Vector3((shapeData.Size.X * 0.5f), (shapeData.Size.Y * 0.5f), -shapeData.Size.Z * 0.5f)
            };
            Sides[2].UV = new Vector4[]
            {
                    new Vector4(1, 0, 0, 0),
                    new Vector4(0, 0, 1, 0)
            };

            //Back
            Sides[3].Plane = new Vector3[]
            {
                    Position + new Vector3(-(shapeData.Size.X * 0.5f), -(shapeData.Size.Y * 0.5f), -shapeData.Size.Z * 0.5f),
                    Position + new Vector3(-(shapeData.Size.X * 0.5f), -(shapeData.Size.Y * 0.5f), shapeData.Size.Z * 0.5f),
                    Position + new Vector3((shapeData.Size.X * 0.5f), -(shapeData.Size.Y * 0.5f), shapeData.Size.Z * 0.5f)
            };
            Sides[3].UV = new Vector4[]
            {
                    new Vector4(1, 0, 0, 0),
                    new Vector4(0, 0, 1, 0)
            };

            //Left
            Sides[4].Plane = new Vector3[]
            {
                    Position + new Vector3(-(shapeData.Size.X * 0.5f), -(shapeData.Size.Y * 0.5f), -shapeData.Size.Z * 0.5f),
                    Position + new Vector3(-(shapeData.Size.X * 0.5f), (shapeData.Size.Y * 0.5f), -shapeData.Size.Z * 0.5f),
                    Position + new Vector3(-(shapeData.Size.X * 0.5f), (shapeData.Size.Y * 0.5f), shapeData.Size.Z * 0.5f)
            };
            Sides[4].UV = new Vector4[]
            {
                    new Vector4(0, 0, 1, 0),
                    new Vector4(0, 1, 0, 0)
            };

            //Right
            Sides[5].Plane = new Vector3[]
            {
                    Position + new Vector3((shapeData.Size.X * 0.5f), -(shapeData.Size.Y * 0.5f), shapeData.Size.Z * 0.5f),
                    Position + new Vector3((shapeData.Size.X * 0.5f), (shapeData.Size.Y * 0.5f), shapeData.Size.Z * 0.5f),
                    Position + new Vector3((shapeData.Size.X * 0.5f), (shapeData.Size.Y * 0.5f), -shapeData.Size.Z * 0.5f)
            };
            Sides[5].UV = new Vector4[]
            {
                    new Vector4(0, 0, 1, 0),
                    new Vector4(0, 1, 0, 0)
            };

            base.GenerateSides(startingID);
        }
    }

    public class ShapeData
    {
        public List<RotationData> Rotation = new List<RotationData>();

        public ShapeData() { }
        public ShapeData(ShapeData self)
        {
            Rotation.Clear();
            for (int i = 0; i < self.Rotation.Count; i++)
            {
                Rotation.Add(new RotationData(self.Rotation[i]));
            }
        }
    }

    public class RotationData
    {
        public Vector3 RotationAxis = new Vector3(0, 0, 0);
        public float RotationAngle = 0;
        public Vector3? RotationPoint = null;
        public bool UsePositionAsCenterPoint = false;

        public RotationData() { }
        public RotationData(RotationData self)
        {
            RotationAxis = self.RotationAxis;
            RotationAngle = self.RotationAngle;
            RotationPoint = self.RotationPoint;
            UsePositionAsCenterPoint = self.UsePositionAsCenterPoint;
        }
    }

    public abstract class Shape
    {
        public Shape() { }
        public Shape(Shape self)
        {
            FuncDetailID = self.FuncDetailID;
            ID = self.ID;
            Position = self.Position;
            Sides = self.Sides;
            Data = new ShapeData(self.Data);
            Texture = self.Texture;
        }

        public int FuncDetailID = -1;
        public int ID;
        public Vector3 Position;
        public SolidSide[] Sides;
        public ShapeData Data;
        public string Texture = Textures.DEV_MEASUREGENERIC01B;

        public virtual void GenerateSides(int startingID)
        {
            Vector3 center = new Vector3(0, 0, 0);
            for (int i = 0; i < Sides.Length; i++)
            {
                Vector3 sideCenter = new Vector3(0, 0, 0);
                for (int j = 0; j < Sides[i].Plane.Length; j++)
                {
                    sideCenter += Sides[i].Plane[j];
                }

                sideCenter /= Sides[i].Plane.Length;

                center += sideCenter;
            }
            center /= Sides.Length;

            while (Data.Rotation.Count > 0)
            {
                RotationData rotData = Data.Rotation[0];
                Data.Rotation.RemoveAt(0);
                if (rotData.UsePositionAsCenterPoint)
                {
                    center = Position;
                }
                if (rotData.RotationPoint != null)
                {
                    center = (Vector3)rotData.RotationPoint;
                }

                Rotate(rotData.RotationAxis, rotData.RotationAngle, center);
            }

            CalculateUVs();
        }

        public virtual void Rotate(Vector3 axis, float angle, Vector3 pointToRotateAround)
        {
            for (int i = 0; i < Sides.Length; i++)
            {
                for (int j = 0; j < Sides[i].Plane.Length; j++)
                {
                    Sides[i].Plane[j] = Vector3.Transform(Sides[i].Plane[j] - pointToRotateAround, Quaternion.CreateFromAxisAngle(axis, angle * (MathF.PI / 180f))) + pointToRotateAround;
                }
            }
        }

        protected virtual void CalculateUVs()
        {
            for (int i = 0; i < Sides.Length; i++)
            {
                Vector3 U = Sides[i].Plane[1] - Sides[i].Plane[0];
                Vector3 V = Sides[i].Plane[2] - Sides[i].Plane[0];

                Vector3 normal = Vector3.Normalize(new Vector3(
                    (U.Y * V.Z) - (U.Z * V.Y),
                    (U.Z * V.X) - (U.X * V.X),
                    (U.X * V.Y) - (U.Y * V.X)
                    ));

                var uAxis = Vector3.Normalize(new Vector3(normal.Z != 0 ? -normal.Z : -normal.Y, 0, normal.X));
                var vAxis = Vector3.Cross(uAxis, normal);

                Sides[i].UV = new Vector4[]
                {
                    new Vector4(uAxis.X, uAxis.Y, uAxis.Z, 0),
                    new Vector4(vAxis.X, vAxis.Y, vAxis.Z, 0)
                };
            }
        }

        public static Vector2 GetNormal2D(Vector2 A, Vector2 B)
        {
            float dx = B.X - A.X;
            float dy = B.Y - A.Y;

            return new Vector2(dy, -dx);
        }
    }

    #endregion

    #region Generators

    public class StairData
    {
        public int FuncDetailId = -1;
        public Vector3 Position;
        public int StairCount;
        public int StairWidth;
        public int Rise;
        public int Run;
        public Direction Direction;
        public int RailingThickness = 0;
    }

    public static class StairsGenerator
    {
        public static List<Shape> Generate(StairData data, RotationData rotationData = null)
        {
            List<Shape> shapes = new List<Shape>();
            Vector3 posChangeDepth = new Vector3();
            Vector3 posChangeWidth = new Vector3();
            float rotationAngle = 0;

            switch (data.Direction)
            {
                case Direction.North:
                    rotationAngle = 90;
                    posChangeDepth = new Vector3(0, -1, -1);
                    posChangeWidth = new Vector3(-1, 0, -1);
                    break;
                case Direction.South:
                    rotationAngle = 270;
                    posChangeDepth = new Vector3(0, 1, -1);
                    posChangeWidth = new Vector3(1, 0, -1);
                    break;
                case Direction.West:
                    rotationAngle = 180;
                    posChangeDepth = new Vector3(1, 0, -1);
                    posChangeWidth = new Vector3(0, 1, -1);
                    break;
                case Direction.East:
                    rotationAngle = 0;
                    posChangeDepth = new Vector3(-1, 0, -1);
                    posChangeWidth = new Vector3(0, -1, -1);
                    break;
            }

            //Each individual step
            for (int i = 0; i < data.StairCount; i++)
            {
                int run = i * data.Run;
                int rise = i * data.Rise;

                switch (data.Direction)
                {
                    case Direction.North:
                        shapes.Add(new Cube()
                        {
                            FuncDetailID = data.FuncDetailId,
                            Position = data.Position + new Vector3(0, run, rise + data.Rise * 0.5f),
                            Data = new CubeShapeData()
                            {
                                Size = new Vector3(data.StairWidth, data.Run, data.Rise)
                            }
                        });
                        break;
                    case Direction.South:
                        shapes.Add(new Cube()
                        {
                            FuncDetailID = data.FuncDetailId,
                            Position = data.Position + new Vector3(0, -run, rise + data.Rise * 0.5f),
                            Data = new CubeShapeData()
                            {
                                Size = new Vector3(data.StairWidth, data.Run, data.Rise)
                            }
                        });
                        break;
                    case Direction.West:
                        shapes.Add(new Cube()
                        {
                            FuncDetailID = data.FuncDetailId,
                            Position = data.Position + new Vector3(-run, 0, rise + data.Rise * 0.5f),
                            Data = new CubeShapeData()
                            {
                                Size = new Vector3(data.Run, data.StairWidth, data.Rise)
                            }
                        });
                        break;
                    case Direction.East:
                        shapes.Add(new Cube()
                        {
                            FuncDetailID = data.FuncDetailId,
                            Position = data.Position + new Vector3(run, 0, rise + data.Rise * 0.5f),
                            Data = new CubeShapeData()
                            {
                                Size = new Vector3(data.Run, data.StairWidth, data.Rise)
                            }
                        });
                        break;
                }
            }

            Cube lastStair = (Cube)shapes[shapes.Count - 1];
            Cube frontPiece = new Cube(lastStair);
            frontPiece.Position.Z = Lerp(shapes[0].Position.Z, lastStair.Position.Z, 0.5f) - data.Rise * 0.5f;
            ((CubeShapeData)frontPiece.Data).Size.Z = data.Rise * (data.StairCount - 1);

            shapes.Add(frontPiece);

            Vector3 perfectlyCenteredPosition = new Vector3(posChangeDepth.X * (data.Run * 1.5f), posChangeDepth.Y * (data.Run * 1.5f), (posChangeDepth.Z * (data.Rise * 0.5f)) + data.Rise * 0.5f);

            //Clip Brush
            shapes.Add(new Polygon()
            {
                FuncDetailID = data.FuncDetailId,
                Texture = Textures.CLIP,
                Position = data.Position + perfectlyCenteredPosition,
                Data = new PolygonShapeData()
                {
                    Rotation = new List<RotationData>()
                    {
                        new RotationData()
                        {
                            RotationAxis = new Vector3(1, 0, 0),
                            RotationAngle = 90,
                            UsePositionAsCenterPoint = true,
                        },
                        new RotationData()
                        {
                            RotationAxis = new Vector3(0, 0, 1),
                            RotationAngle = rotationAngle,
                            UsePositionAsCenterPoint = true
                        }
                    },
                    Depth = data.StairWidth,
                    Scalar = 1,
                    PolygonPoints = new List<Vector2>()
                    {
                        new Vector2(0, 0),
                        new Vector2(data.Run * data.StairCount, 0),
                        new Vector2(data.Run * data.StairCount, data.Rise * data.StairCount)
                    }
                }
            });

            if (data.RailingThickness != 0)
            {
                //Railing Left
                shapes.Add(new Polygon()
                {
                    FuncDetailID = data.FuncDetailId,
                    Texture = Textures.DEV_MEASUREGENERIC01B,
                    Position = data.Position + perfectlyCenteredPosition + posChangeWidth * new Vector3(data.StairWidth * 0.5f + data.RailingThickness * 0.5f, data.StairWidth * 0.5f + data.RailingThickness * 0.5f, 0),
                    Data = new PolygonShapeData()
                    {
                        Rotation = new List<RotationData>()
                        {
                            new RotationData()
                            {
                                RotationAxis = new Vector3(1, 0, 0),
                                RotationAngle = 90,
                                UsePositionAsCenterPoint = true,
                            },
                            new RotationData()
                            {
                                RotationAxis = new Vector3(0, 0, 1),
                                RotationAngle = rotationAngle,
                                UsePositionAsCenterPoint = true
                            }
                        },
                        Depth = data.RailingThickness,
                        Scalar = 1,
                        PolygonPoints = new List<Vector2>()
                        {
                            new Vector2(0, 0),
                            new Vector2(data.Run * data.StairCount + data.Run, 0),
                            new Vector2(data.Run * data.StairCount + data.Run, data.Rise * data.StairCount),
                            new Vector2(data.Run * data.StairCount, data.Rise * data.StairCount),
                        }
                    }
                });

                //Railing Right
                shapes.Add(new Polygon()
                {
                    FuncDetailID = data.FuncDetailId,
                    Texture = Textures.DEV_MEASUREGENERIC01B,
                    Position = data.Position + perfectlyCenteredPosition + posChangeWidth * new Vector3(-data.StairWidth * 0.5f - data.RailingThickness * 0.5f, -data.StairWidth * 0.5f - data.RailingThickness * 0.5f, 0),
                    Data = new PolygonShapeData()
                    {
                        Rotation = new List<RotationData>()
                        {
                            new RotationData()
                            {
                                RotationAxis = new Vector3(1, 0, 0),
                                RotationAngle = 90,
                                UsePositionAsCenterPoint = true,
                            },
                            new RotationData()
                            {
                                RotationAxis = new Vector3(0, 0, 1),
                                RotationAngle = rotationAngle,
                                UsePositionAsCenterPoint = true
                            }
                        },
                        Depth = data.RailingThickness,
                        Scalar = 1,
                        PolygonPoints = new List<Vector2>()
                        {
                            new Vector2(0, 0),
                            new Vector2(data.Run * data.StairCount + data.Run, 0),
                            new Vector2(data.Run * data.StairCount + data.Run, data.Rise * data.StairCount),
                            new Vector2(data.Run * data.StairCount, data.Rise * data.StairCount),
                        }
                    }
                });
            }

            rotationData.RotationPoint = data.Position;

            if (rotationData != null)
            {
                for (int i = 0; i < shapes.Count; i++)
                {
                    shapes[i].Data.Rotation.Add(rotationData);
                }
            }


            return shapes;
        }
        private static float Lerp(float firstFloat, float secondFloat, float by)
        {
            return firstFloat * (1 - by) + secondFloat * by;
        }
    }

    public class WallData
    {
        public int Height;
        public int Thickness;
        public bool CapEnd = true;
        public List<int> facesIndicesToSkip = new List<int>();
    }

    public static class WallGenerator
    {
        public static List<Polygon> CreateWalls(Polygon polygon, WallData wallData)
        {
            List<List<Vector2>> allPoints = new List<List<Vector2>>();
            List<Vector2> points = new List<Vector2>();
            List<Vector2> lastFace = new List<Vector2>();
            List<List<Vector2>> normals = new List<List<Vector2>>();

            PolygonShapeData polyData = polygon.Data as PolygonShapeData;

            List<Vector2> sharedValues = polyData.PolygonPoints.GroupBy(x => x).Where(g => g.Count() > 1).Select(x => x.Key).ToList();
            for (int i = 0; i < polyData.PolygonPoints.Count; i++)
            {
                if(sharedValues.Contains(polyData.PolygonPoints[i]))
                {
                    wallData.facesIndicesToSkip.Add(i);
                    i++;
                }
            }

            for (int i = 0; i < polyData.PolygonPoints.Count; i++)
            {
                if (wallData.facesIndicesToSkip.Contains(i))
                {
                    continue;
                }

                int index1 = (i - 1) % polyData.PolygonPoints.Count;
                if (index1 == -1)
                {
                    index1 = polyData.PolygonPoints.Count - 1;
                }
                int index2 = i % polyData.PolygonPoints.Count;
                int index3 = (i + 1) % polyData.PolygonPoints.Count;
                int index4 = (i + 2) % polyData.PolygonPoints.Count;
                Vector2 a = polyData.PolygonPoints[index1] * polyData.Scalar;
                Vector2 b = polyData.PolygonPoints[index2] * polyData.Scalar;
                Vector2 c = polyData.PolygonPoints[index3] * polyData.Scalar;
                Vector2 d = polyData.PolygonPoints[index4] * polyData.Scalar;

                Vector2 abNormal = Vector2.Normalize(Shape.GetNormal2D(a, b));
                Vector2 bcNormal = Vector2.Normalize(Shape.GetNormal2D(b, c));
                Vector2 vertexNormalabc = Vector2.Normalize((abNormal + bcNormal)) * wallData.Thickness;
                Vector2 cdNormal = Vector2.Normalize(Shape.GetNormal2D(c, d));
                Vector2 vertexNormalbcd = Vector2.Normalize((bcNormal + cdNormal)) * wallData.Thickness;

                Vector2 point = polyData.PolygonPoints[index2] * polyData.Scalar + vertexNormalabc;
                Vector2 point2 = polyData.PolygonPoints[index3] * polyData.Scalar + vertexNormalbcd;
                points.Add(point);
                points.Add(point2);
                points.Add(c);
                points.Add(b);

                //for (int j = 0; j < points.Count; j++)
                //{
                //    points[j] /= 128;
                //}

                if (wallData.facesIndicesToSkip.Contains(i + 1))
                {
                    LineEquation first = new LineEquation(points[2], points[2] - bcNormal);
                    LineEquation second = new LineEquation(points[0], points[1]);
                    Vector2 intersectPoint;
                    if(first.IntersectsWithLine(second, out intersectPoint))
                    {
                        points[1] = intersectPoint;
                    }
                }
                if(wallData.facesIndicesToSkip.Contains(i - 1))
                {
                    LineEquation first = new LineEquation(points[3], points[3] - bcNormal);
                    LineEquation second = new LineEquation(points[0], points[1]);
                    Vector2 intersectPoint;
                    if (first.IntersectsWithLine(second, out intersectPoint))
                    {
                        points[0] = intersectPoint;
                    }
                }

                allPoints.Add(points);
                points = new List<Vector2>();
            }

            //for (int i = 0; i < allPoints.Count; i++)
            //{
            //    for (int j = 0; j < allPoints[i].Count; j++)
            //    {
            //        allPoints[i][j] /= 128;
            //    }
            //}

            List<Polygon> finalPolygons = new List<Polygon>();
            for (int i = 0; i < allPoints.Count; i++)
            {
                finalPolygons.Add(
                    new Polygon()
                    {
                        Position = polygon.Position + new Vector3(0, 0, polygon.Position.Z * 0.5f),
                        Data = new PolygonShapeData()
                        {
                            Depth = wallData.Height,
                            Scalar = 1,
                            PolygonPoints = allPoints[i]
                        }
                    });
            }

            return finalPolygons;

            #region Old
#if false
            for (int i = 0; i < polyData.PolygonPoints.Count; i++)
            {
                int index1 = i - 1;
                if(index1 == -1)
                {
                    index1 = polyData.PolygonPoints.Count - 1;
                }
                int index2 = i;
                int index3 = (i + 1) % polyData.PolygonPoints.Count;
                Vector2 a = polyData.PolygonPoints[index1] * polyData.Scalar;
                Vector2 b = polyData.PolygonPoints[index2] * polyData.Scalar;
                Vector2 c = polyData.PolygonPoints[index3] * polyData.Scalar;
                Vector2 abNormal = Vector2.Normalize(Shape.GetNormal2D(a, b));
                Vector2 bcNormal = Vector2.Normalize(Shape.GetNormal2D(b, c));
                Vector2 vertexNormal = Vector2.Normalize((abNormal + bcNormal)) * wallData.Thickness;

                normals.Add(new List<Vector2>()
                {
                    Vector2.Lerp(a, b, 0.5f),
                    Vector2.Lerp(a, b, 0.5f) + abNormal * (polyData.Scalar * 0.5f),
                });
                normals.Add(new List<Vector2>()
                {
                    Vector2.Lerp(b, c, 0.5f),
                    Vector2.Lerp(b, c, 0.5f) + bcNormal * (polyData.Scalar * 0.5f),
                });

                Vector2 point = polyData.PolygonPoints[i] * polyData.Scalar + vertexNormal;
                points.Add(point);
                normals.Add(new List<Vector2>()
                {
                    polyData.PolygonPoints[i] * polyData.Scalar,
                    point
                });
                if (i == 0 || i == polyData.PolygonPoints.Count - 1)
                {
                    lastFace.Add(point);
                }
            }

            for (int i = polyData.PolygonPoints.Count - 1; i >= 0; i--)
            {
                Vector2 point = polyData.PolygonPoints[i] * polyData.Scalar;
                points.Add(point);

                if (i == 0 || i == polyData.PolygonPoints.Count - 1)
                {
                    lastFace.Add(point);
                }
            }

            lastFace = new List<Vector2>()
            {
                lastFace[1],
                lastFace[0],
                lastFace[3],
                lastFace[2]
            };

            float scale = 0.3f;
            Pen whitePen = new Pen(Color.White, 3);
            Pen greyPen = new Pen(Color.Gray, 3);
            Pen blackPen = new Pen(Color.Black, 3);
            Pen redPen = new Pen(Color.Red, 3);
            Pen bluePen = new Pen(Color.Blue, 3);
            Pen greenPen = new Pen(Color.Green, 3);
            using (Bitmap canvas = new Bitmap(500, 500))
            {
                using (Graphics g = Graphics.FromImage(canvas))
                {
                    g.FillRectangle(Brushes.White, new Rectangle(0, 0, 500, 500));

                    for (int j = 0; j < points.Count; j++)
                    {
                        int iN = (j + 1) % points.Count;
                        Point p1 = new Point((int)(points[j].X * scale + 100), (int)(points[j].Y * scale + 100));
                        Point p2 = new Point((int)(points[iN].X * scale + 100), (int)(points[iN].Y * scale + 100));
                    
                        g.DrawLine(j < polyData.PolygonPoints.Count ? greyPen : blackPen, p1, p2);
                    }
                    
                    for (int j = 0; j < normals.Count; j++)
                    {
                        g.DrawLine(j % 3 == 2 ? redPen : bluePen, new Point((int)(normals[j][0].X * scale) + 100, (int)(normals[j][0].Y * scale) + 100), new Point((int)(normals[j][1].X * scale) + 100, (int)(normals[j][1].Y * scale) + 100));
                    }
                }
            
                canvas.Save(@"C:\Users\funny\source\repos\VMFConverter\Gen" + (0) + ".png");
            }

            return new List<Polygon>()
            {
                new Polygon()
                {
                    Position = polygon.Position + new Vector3(0, 0, polygon.Position.Z * 0.5f),
                    Data = new PolygonShapeData()
                    {
                        Depth = wallData.Height,
                        Scalar = 1,
                        PolygonPoints = points
                    }
                },
                new Polygon()
                {
                    Position = polygon.Position + new Vector3(0, 0, polygon.Position.Z * 0.5f),
                    Data = new PolygonShapeData()
                    {
                        Depth = wallData.Height,
                        Scalar = 1,
                        PolygonPoints = lastFace
                    }
                }
            };
#endif
            #endregion
        }
    }

    #endregion
}
