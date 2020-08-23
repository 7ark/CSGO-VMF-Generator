﻿using System;
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
}";

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
    ""comment"" ""Created with 7ark's VMF Generator, found at https://github.com/7ark/CSGO-VMF-Generator""
    ""detailmaterial"" ""detail/detailsprites""
    ""detailvbsp"" ""detail.vbsp""
    ""maxpropscreenwidth"" ""-1""
    ""skyname"" ""sky_cs15_daylight02_hdr""
    ";
        #endregion

        public string Generate(string path)
        {
            string uniqueVMF = string.Empty;

            List<Shape> shapes = new List<Shape>();
            List<string> entities = new List<string>();
            GenerateData(out shapes, out entities);

            List<string> visgroups = new List<string>();

            for (int i = 0; i < shapes.Count; i++)
            {
                if(!visgroups.Contains(shapes[i].Visgroup))
                {
                    visgroups.Add(shapes[i].Visgroup);
                }
            }

            Random rand = new Random();
            for (int i = 0; i < visgroups.Count; i++)
            {
                Visgroups.VisgroupNameToID.Add(visgroups[i], i);
                Visgroups.VisgroupNameToColor.Add(visgroups[i], Color.FromArgb(rand.Next(0, 255), rand.Next(0, 255), rand.Next(0, 255)));
            }

            List<Shape> brushes = new List<Shape>();
            Dictionary<int, List<Shape>> funcDetails = new Dictionary<int, List<Shape>>();

            //Seperate the brushes from the func details
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
            for (int i = 0; i < visgroups.Count; i++)
            {
                uniqueVMF += "\tvisgroup" + Environment.NewLine;
                uniqueVMF += "\t{" + Environment.NewLine;
                uniqueVMF += "\t\t\"name\" \"" + visgroups[i] + "\"" + Environment.NewLine;
                uniqueVMF += "\t\t\"visgroupid\" \"" + Visgroups.VisgroupNameToID[visgroups[i]] + "\"" + Environment.NewLine;
                Color col = Visgroups.VisgroupNameToColor[visgroups[i]];
                uniqueVMF += "\t\t\"color\" \"" + col.R + " " + col.G + " " + col.B + "\"" + Environment.NewLine;
                uniqueVMF += "\t}" + Environment.NewLine;
            }
            uniqueVMF += "}" + Environment.NewLine;
            uniqueVMF += viewSettingsConstant + Environment.NewLine;
            uniqueVMF += "world" + Environment.NewLine;
            uniqueVMF += "{" + Environment.NewLine;
            uniqueVMF += worldSetupConstant + Environment.NewLine;

            WriteShapes(ref uniqueVMF, brushes, false);

            uniqueVMF += "}" + Environment.NewLine;

            //Generic entities
            for (int i = 0; i < entities.Count; i++)
            {
                uniqueVMF += entities[i] + Environment.NewLine;
            }

            //Func details
            foreach (int id in funcDetails.Keys)
            {
                uniqueVMF += "entity" + Environment.NewLine;
                uniqueVMF += "{" + Environment.NewLine;
                uniqueVMF += "\t\"id\" \"" + (++EntityTemplates.LastID) + "\"" + Environment.NewLine;
                uniqueVMF += "\t\"classname\" \"func_detail\"" + Environment.NewLine;
                WriteShapes(ref uniqueVMF, funcDetails[id], true);
                uniqueVMF += "}" + Environment.NewLine;
            }

            return uniqueVMF;
        }

        private void WriteShapes(ref string uniqueVMF, List<Shape> shapes, bool writeVisgroupsAfter)
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
                if(!writeVisgroupsAfter)
                {
                    WriteVisgroups(ref uniqueVMF, shapes[i].Visgroup);
                }
                uniqueVMF += "\t}" + Environment.NewLine;
            }

            if(shapes.Count > 0)
            {
                //Not too concerned about setting up multiple visgroups for now, can be done later if needed
                string visgroup = shapes[0].Visgroup;

                if (writeVisgroupsAfter)
                {
                    WriteVisgroups(ref uniqueVMF, visgroup, 2);
                }
            }
        }

        private void WriteVisgroups(ref string uniqueVMF, string visgroup, int indents = 3)
        {
            uniqueVMF += new string('\t', indents - 1) + "editor" + Environment.NewLine;
            uniqueVMF += new string('\t', indents - 1) + "{" + Environment.NewLine;

            string visgroupName = visgroup;
            int visgroupId = -1;
            Color col = Color.White;

            if (visgroupName != string.Empty)
            {
                visgroupId = Visgroups.VisgroupNameToID[visgroupName];
                col = Visgroups.VisgroupNameToColor[visgroupName];
            }

            uniqueVMF += new string('\t', indents) + "\"color\" \"" + col.R + " " + col.G + " " + col.B + "\"" + Environment.NewLine;
            if (visgroupId != -1)
            {
                uniqueVMF += new string('\t', indents) + "\"visgroupid\" \"" + visgroupId + "\"" + Environment.NewLine;
            }
            uniqueVMF += new string('\t', indents) + "\"visgroupshown\" \"1\"" + Environment.NewLine;
            uniqueVMF += new string('\t', indents) + "\"visgroupautoshown\" \"1\"" + Environment.NewLine;
            uniqueVMF += new string('\t', indents - 1) + "}" + Environment.NewLine;
        }

        private void GenerateData(out List<Shape> shapesResult, out List<string> entities)
        {
            List<GenerationMethod> generationMethods = new List<GenerationMethod>();
            entities = new List<string>();
            EasyInputLayer.GetInput(out generationMethods, out entities);

            //Start shape construction
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
                                    VMFDebug.AddShapeToGraphics(g, shapes[j] as Polygon, greyPen, positionAdjustment: new Vector2(250, 50), scale: 0.14f);
                                }
                            }
                            VMFDebug.AddShapeToGraphics(g, shapes[i] as Polygon, redPen, positionAdjustment: new Vector2(250, 50), scale: 0.14f);
                        });


                        Console.WriteLine("Found a concave shape. Attempting triangulation");
                        List<Polygon> replacements = new List<Polygon>();
                        List<Shape> temp = ConvertToConvex(shapes[i] as Polygon);

                        for (int j = 0; j < temp.Count; j++)
                        {
                            replacements.Add(temp[j] as Polygon);
                        }

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

            int currId = 0;
            for (int i = 0; i < finalShapeList.Count; i++)
            {
                finalShapeList[i].ID = i;
                finalShapeList[i].GenerateSides(currId);
                currId += finalShapeList[i].Sides.Length;
            }
            //End shape construction

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

                    Vector2 posAdjust = new Vector2(250, 50);
                    float scale = 0.14f;

                    for (int i = 0; i < options.Count; i++)
                    {
                        VMFDebug.AddShapeToGraphics(g, options[i], greyPen, positionAdjustment: posAdjust, scale: scale);
                    }

                    for (int i = 0; i < applicants.Count; i++)
                    {
                        VMFDebug.AddShapeToGraphics(g, applicants[i], blackPen, positionAdjustment: posAdjust, scale: scale);
                    }

                    if (found != null)
                    {
                        VMFDebug.AddShapeToGraphics(g, found, bluePen, positionAdjustment: posAdjust, scale: scale);
                    }

                    if (prev != null)
                    {
                        VMFDebug.AddShapeToGraphics(g, prev, greenPen, positionAdjustment: posAdjust, scale: scale);
                    }
                    else
                    {
                        VMFDebug.AddShapeToGraphics(g, currentPolygonToEval, redPen, positionAdjustment: posAdjust, scale: scale);
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
