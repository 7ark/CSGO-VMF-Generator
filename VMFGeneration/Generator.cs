using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using VMFGenerator;

namespace VMFConverter
{
    public class Generator
    {
        #region Constants
        private const string versionInfoConstant =
            @"versioninfo
{
	""editorversion"" ""400""
	""editorbuild"" ""8456""
	""mapversion"" ""1""
	""formatversion"" ""100""
	""prefab"" ""0""
}
            ";

        private const string viewSettingsConstant =
            @"viewsettings
{
      ""bSnapToGrid"" ""1""
      ""bShowGrid"" ""1""
      ""bShowLogicalGrid"" ""0""
      ""nGridSpacing"" ""64""
      ""bShow3DGrid"" ""0""
}";

        private const string worldSetupConstant =
            @"    ""id"" ""1""
    ""mapversion"" ""797""
    ""classname"" ""worldspawn""
    ""detailmaterial"" ""detail/detailsprites""
    ""detailvbsp"" ""detail.vbsp""
    ""maxpropscreenwidth"" ""-1""
    ""skyname"" ""sky_cs15_daylight02_hdr""
    ";
        #endregion

        public string Generate(string path)
        {
            string uniqueVMF = string.Empty;

            List<Shape> shapes = null;
            GenerateData(out shapes);

            List<string> entities = new List<string>();
            entities.Add(EntityTemplates.LightEnvironment(
                lightColor: Color.FromArgb(255, 240, 240),
                ambientLightColor: Color.FromArgb(240, 240, 255),
                origin: new Vector3(0, 0, 256),
                brightness: 400,
                angles: new Vector3(-40, -60, 0),
                pitch: -60));

            List<Shape> brushes = new List<Shape>();
            Dictionary<int, List<Shape>> funcDetails = new Dictionary<int, List<Shape>>();

            for (int i = 0; i < shapes.Count; i++)
            {
                if (shapes[i].FuncDetailID == -1)
                {
                    brushes.Add(shapes[i]);
                }
                else
                {
                    if (!funcDetails.ContainsKey(shapes[i].FuncDetailID))
                    {
                        funcDetails.Add(shapes[i].FuncDetailID, new List<Shape>());
                    }

                    funcDetails[shapes[i].FuncDetailID].Add(shapes[i]);
                }
            }

            uniqueVMF += versionInfoConstant + Environment.NewLine;
            uniqueVMF += "visgroups" + Environment.NewLine;
            uniqueVMF += "{" + Environment.NewLine;
            uniqueVMF += "}" + Environment.NewLine;
            uniqueVMF += viewSettingsConstant + Environment.NewLine;
            uniqueVMF += "world" + Environment.NewLine;
            uniqueVMF += "{" + Environment.NewLine;
            uniqueVMF += worldSetupConstant + Environment.NewLine;

            WriteShapes(ref uniqueVMF, brushes);

            uniqueVMF += "}" + Environment.NewLine;

            //Generic entities
            for (int i = 0; i < entities.Count; i++)
            {
                uniqueVMF += entities[i] + Environment.NewLine;
            }

            int currEntityId = EntityTemplates.LastID++;

            //Func details

            foreach (int id in funcDetails.Keys)
            {
                uniqueVMF += "entity" + Environment.NewLine;
                uniqueVMF += "{" + Environment.NewLine;
                uniqueVMF += "\t\"id\" \"" + currEntityId + "\"" + Environment.NewLine;
                uniqueVMF += "\t\"classname\" \"func_detail\"" + Environment.NewLine;
                WriteShapes(ref uniqueVMF, funcDetails[id]);
                uniqueVMF += "}" + Environment.NewLine;

                currEntityId++;
            }


            return uniqueVMF;
        }

        private void WriteShapes(ref string uniqueVMF, List<Shape> shapes)
        {
            //Brushes
            for (int i = 0; i < shapes.Count; i++)
            {
                uniqueVMF += "\tsolid" + Environment.NewLine;
                uniqueVMF += "\t{" + Environment.NewLine;
                uniqueVMF += "\t\t\"id\" \"" + shapes[i].ID + "\"" + Environment.NewLine;
                SolidSide[] sides = shapes[i].Sides;
                for (int j = 0; j < sides.Length; j++)
                {
                    uniqueVMF += "\t\tside" + Environment.NewLine;
                    uniqueVMF += "\t\t{" + Environment.NewLine;
                    uniqueVMF += "\t\t\t\"id\" \"" + sides[j].ID + "\"" + Environment.NewLine +
                    "\t\t\t\"plane\" \"(" + sides[j].Plane[0].X + " " + sides[j].Plane[0].Y + " " + sides[j].Plane[0].Z + ") (" + sides[j].Plane[1].X + " " + sides[j].Plane[1].Y + " " + sides[j].Plane[1].Z + ") (" + sides[j].Plane[2].X + " " + sides[j].Plane[2].Y + " " + sides[j].Plane[2].Z + ")\"" + Environment.NewLine +
                    "\t\t\t\"material\" \"" + shapes[i].Texture + "\"" + Environment.NewLine +
                    "\t\t\t\"uaxis\" \"[" + sides[j].UV[0].X + " " + sides[j].UV[0].Y + " " + sides[j].UV[0].Z + " " + sides[j].UV[0].W + "] 0.25\"" + Environment.NewLine +
                    "\t\t\t\"vaxis\" \"[" + sides[j].UV[1].X + " " + sides[j].UV[1].Y + " " + sides[j].UV[1].Z + " " + sides[j].UV[1].W + "] 0.25\"" + Environment.NewLine +
                    "\t\t\t\"rotation\" \"0\"" + Environment.NewLine +
                    "\t\t\t\"lightmapscale\" \"16\"" + Environment.NewLine +
                    "\t\t\t\"smoothing_groups\" \"0\"" + Environment.NewLine;
                    uniqueVMF += "\t\t}" + Environment.NewLine;
                }
                uniqueVMF += "\t}" + Environment.NewLine;
            }
        }

        private void GenerateData(out List<Shape> shapesResult)
        {
            List<GenerationMethod> generationMethods = new List<GenerationMethod>();
            //generationMethods.Add(new MiscGenerationMethod());
            generationMethods.Add(new ImageGenerationMethod()
            {
                Detail = 20,
                InputFilePath = @"C:\Users\funny\source\repos\VMFGenerator\Input\InputTest2.png"
            });
            generationMethods.Add(new HollowCubeGenerationMethod()
            {
                Position = new Vector3(0, 0, 5f),
                Texture = Textures.SKYBOX,
                Scalar = 64,
                Size = new Vector3(50, 50, 20),
                Thickness = 0.5f
            });

            List<Shape> shapes = new List<Shape>();
            for (int i = 0; i < generationMethods.Count; i++)
            {
                shapes.AddRange(generationMethods[i].GetBrushes());
            }

            List<Shape> finalShapeList = new List<Shape>();
            for (int i = shapes.Count - 1; i >= 0; i--)
            {
                if (shapes[i] is Polygon)
                {
                    (shapes[i] as Polygon).CalculatePreGenerateData();

                    if (!IsShapeConvex(shapes[i] as Polygon))
                    {
                        VMFDebug.CreateDebugImage("PreTriangulation" + (i), onDraw: (g) =>
                        {
                            Pen greyPen = new Pen(Color.Gray, 3);
                            Pen redPen = new Pen(Color.Red, 3);
                            for (int j = 0; j < shapes.Count; j++)
                            {
                                if (shapes[j] is Polygon)
                                {
                                    VMFDebug.AddShapeToGraphics(g, shapes[j] as Polygon, greyPen);
                                }
                            }
                            VMFDebug.AddShapeToGraphics(g, shapes[i] as Polygon, redPen);
                        });


                        Console.WriteLine("Found a concave shape. Attempting triangulation");
                        List<Polygon> replacements = new List<Polygon>();
                        List<Shape> temp = ConvertToConvex(shapes[i] as Polygon);

                        for (int j = 0; j < temp.Count; j++)
                        {
                            replacements.Add(temp[j] as Polygon);
                        }

                        VMFDebug.CreateDebugImage("AfterSplit" + (i), onDraw: (g) =>
                        {
                            Pen greyPen = new Pen(Color.Gray, 3);
                            Pen redPen = new Pen(Color.Red, 3);
                            for (int j = 0; j < replacements.Count; j++)
                            {
                                if (replacements[j] is Polygon)
                                {
                                    VMFDebug.AddShapeToGraphics(g, replacements[j] as Polygon, greyPen);
                                }
                            }
                            VMFDebug.AddShapeToGraphics(g, shapes[i] as Polygon, redPen);
                        });

                        Console.WriteLine("Single shape converted into " + replacements.Count + " new shapes");

                        List<Polygon> combinedShapes = new List<Polygon>();

                        for (int j = replacements.Count - 1; j >= 0; j--)
                        {
                            if (!IsShapeConvex(replacements[j] as Polygon))
                            {
                                replacements[j] = RemoveRedundantPoints(replacements[j] as Polygon);
                                Console.WriteLine("An invalid shape was found in the replacement batch. Attempting to fix...");

                                if (((PolygonShapeData)replacements[j].Data).PolygonPoints.Count < 3 || !IsShapeConvex(replacements[j] as Polygon))
                                {
                                    Console.WriteLine("Could not fix invalid shape. Deleting.");
                                    replacements.RemoveAt(j);
                                }
                                else
                                {
                                    Console.WriteLine("Invalid shape fixed!");
                                }
                            }
                        }

                        combinedShapes = replacements;
                        CombineShapes(replacements, out combinedShapes);

                        PolygonShapeData shapeData = shapes[i].Data as PolygonShapeData;
                        for (int j = 0; j < combinedShapes.Count; j++)
                        {
                            finalShapeList.Add(combinedShapes[j]);
                        }
                    }
                    else
                    {
                        finalShapeList.Add(shapes[i] as Polygon);
                    }
                }
                else
                {
                    finalShapeList.Add(shapes[i]);
                }
            }

            //CombineShapes(finalShapeList, out finalShapeList);

            int currId = 0;
            for (int i = 0; i < finalShapeList.Count; i++)
            {
                finalShapeList[i].ID = i;
                finalShapeList[i].GenerateSides(currId);
                currId += finalShapeList[i].Sides.Length;
            }

            shapesResult = finalShapeList;
        }

        public static Polygon RemoveRedundantPoints(Polygon polygon, bool smallDetail = false)
        {
            List<Vector2> points = ((PolygonShapeData)polygon.Data).PolygonPoints;
            Vector2 one = points[0];
            Vector2 two = points[1];
            Vector2 three = points[2];

            List<int> indicesToRemove = new List<int>();

            for (int i = 0; i < points.Count + 2; i++)
            {
                int index = i % points.Count;
                int index2 = (i + 1) % points.Count;
                int index3 = (i + 2) % points.Count;

                one = points[index];
                two = points[index2];
                three = points[index3];

                Vector2 oneThreeDirection = Vector2.Normalize(three - one);
                Vector2 oneTwoDirection = Vector2.Normalize(two - one);
                Vector2 twoThreeDirection = Vector2.Normalize(two - one);

                float dot1 = Vector2.Dot(oneThreeDirection, oneTwoDirection);

                bool same = (oneThreeDirection == oneTwoDirection && oneThreeDirection == twoThreeDirection) || (smallDetail && dot1 > 0.5f);
                if (same && !indicesToRemove.Contains(i))
                {
                    indicesToRemove.Add(index2);
                }
            }

            for (int i = points.Count; i >= 0; i--)
            {
                if (indicesToRemove.Contains(i))
                {
                    points.RemoveAt(i);
                }
            }

            Polygon final = new Polygon(polygon);
            ((PolygonShapeData)final.Data).PolygonPoints = points;
            return final;
        }


        public static void CombineShapes(List<Polygon> shapes, out List<Polygon> resultingShapes, int depth = 0)
        {
            List<Polygon> applicants = new List<Polygon>();

            Polygon currentPolygonToEval = null;
            List<Polygon> options = new List<Polygon>(shapes);
            int save = 0;

            //for (int i = 0; i < options.Count; i++)
            //{
            //    for (int j = 0; j < ((PolygonShapeData)options[i].Data).PolygonPoints.Count; j++)
            //    {
            //        ((PolygonShapeData)options[i].Data).PolygonPoints[j] /= 64;
            //    }
            //}

            while (true)
            {
                if (currentPolygonToEval == null)
                {
                    if (options.Count <= 0)
                    {
                        break;
                    }
                    currentPolygonToEval = options[0];
                    options.RemoveAt(0);
                }

                Polygon prev = null;
                Polygon found = null;
                bool anyCombination = false;
                for (int i = 0; i < options.Count; i++)
                {
                    Polygon v = new Polygon(currentPolygonToEval);
                    v = v.Combine(options[i]);

                    if (v != null)
                    {
                        v = RemoveRedundantPoints(v);
                        if (IsShapeConvex(v))
                        {
                            found = options[i];
                            prev = new Polygon(currentPolygonToEval);
                            currentPolygonToEval = v;
                            options.RemoveAt(i);
                            anyCombination = true;
                            break;
                        }
                    }
                }

                VMFDebug.CreateDebugImage("CombiningStep" + (save++), onDraw: (g) =>
                {
                    Pen greyPen = new Pen(Color.Gray, 3);
                    Pen blackPen = new Pen(Color.Black, 3);
                    Pen redPen = new Pen(Color.Red, 3);
                    Pen bluePen = new Pen(Color.Blue, 3);
                    Pen greenPen = new Pen(Color.Green, 3);

                    for (int i = 0; i < options.Count; i++)
                    {
                        VMFDebug.AddShapeToGraphics(g, options[i], greyPen);
                    }

                    for (int i = 0; i < applicants.Count; i++)
                    {
                        VMFDebug.AddShapeToGraphics(g, applicants[i], blackPen);
                    }

                    if (found != null)
                    {
                        VMFDebug.AddShapeToGraphics(g, found, bluePen);
                    }

                    if (prev != null)
                    {
                        VMFDebug.AddShapeToGraphics(g, prev, greenPen);
                    }
                    else
                    {
                        VMFDebug.AddShapeToGraphics(g, currentPolygonToEval, redPen);
                    }
                });

                if (!anyCombination)
                {
                    applicants.Add(new Polygon(currentPolygonToEval));
                    currentPolygonToEval = null;
                }
            }

            List<Polygon> result = new List<Polygon>();
            for (int i = 0; i < applicants.Count; i++)
            {
                bool canAdd = true;
                for (int j = 0; j < result.Count; j++)
                {
                    PolygonShapeData rSD = result[j].Data as PolygonShapeData;
                    PolygonShapeData aSD = applicants[i].Data as PolygonShapeData;
                    if (rSD.PolygonPoints.SequenceEqual(aSD.PolygonPoints))
                    {
                        canAdd = false;
                        break;
                    }
                }
                if (canAdd)
                {
                    result.Add(applicants[i]);
                }
            }

            resultingShapes = result;
        }

        private List<Shape> ConvertToConvex(Polygon shape)
        {
            PolygonShapeData shapeData = shape.Data as PolygonShapeData;

            List<List<Vector2>> result = PolygonTriangulator.Triangulate(shapeData.PolygonPoints);
            List<Shape> newShapeList = new List<Shape>();

            for (int i = 0; i < result.Count; i++)
            {
                Polygon newShape = new Polygon(shape);
                List<Vector2> points = result[i];
                //points.Reverse();
                newShape.Data = new PolygonShapeData()
                {
                    Depth = shapeData.Depth,
                    Scalar = shapeData.Scalar,
                    PolygonPoints = points
                };
                newShapeList.Add(newShape);
            }

            return newShapeList;
        }

        public static bool IsShapeConvex(Polygon shape)
        {
            PolygonShapeData shapeData = shape.Data as PolygonShapeData;

            for (int i = 0; i < shapeData.PolygonPoints.Count; i++)
            {
                Vector2 first = shapeData.PolygonPoints[i];
                int sI = i == shapeData.PolygonPoints.Count - 1 ? 0 : i + 1;
                Vector2 second = shapeData.PolygonPoints[sI];
                for (int j = 0; j < shapeData.PolygonPoints.Count; j++)
                {
                    Vector2 pointToCheck = shapeData.PolygonPoints[j];
                    if (pointToCheck != first && pointToCheck != second)
                    {
                        bool isConvex = ((second.X - first.X) * (pointToCheck.Y - first.Y) - (second.Y - first.Y) * (pointToCheck.X - first.X)) > 0;

                        if (!isConvex)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
