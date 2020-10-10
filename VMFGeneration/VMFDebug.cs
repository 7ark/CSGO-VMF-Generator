using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Text;
using VMFGenerator;

namespace VMFGenerator
{
    public static class VMFDebug
    {
        public static bool DebugMode = false;

        public static void CreateDebugImage(string fileName, System.Action<Graphics> onDraw, int width = 500, int height = 500)
        {
            if(!DebugMode)
            {
                return;
            }

            using (Bitmap canvas = new Bitmap(width, height))
            {
                using (Graphics g = Graphics.FromImage(canvas))
                {
                    g.FillRectangle(Brushes.White, new Rectangle(0, 0, width, height));

                    onDraw?.Invoke(g);
                }

                if(!Directory.Exists(Directory.GetCurrentDirectory() + @"\DebugOutput"))
                {
                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\DebugOutput");
                }

                canvas.Save(Directory.GetCurrentDirectory() + @"\DebugOutput\" + fileName + ".png");
            }
        }
        public static void AddShapeToGraphics(Graphics g, Polygon polgon, Pen pen, Vector2 positionAdjustment = new Vector2(), float scale = 0.1f)
        {
            List<Vector2> points = ((PolygonShapeData)polgon.Data).PolygonPoints;
            for (int i = 0; i < points.Count; i++)
            {
                int iN = (i + 1) % points.Count;
                Point p1 = new Point((int)(points[i].X * scale + positionAdjustment.X), (int)(points[i].Y * scale + positionAdjustment.Y));
                Point p2 = new Point((int)(points[iN].X * scale + positionAdjustment.X), (int)(points[iN].Y * scale + positionAdjustment.Y));

                g.DrawLine(pen, p1, p2);
            }
        }
    }
}
