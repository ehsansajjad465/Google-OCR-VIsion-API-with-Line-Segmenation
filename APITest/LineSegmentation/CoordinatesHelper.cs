﻿using Google.Apis.Vision.v1.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCRAPITest.LineSegmentation
{
    internal class CoordinatesHelper
    {
        /// <summary>
        /// Inverts the Y axis coordinates for easier computation
        /// as the google vision starts the Y axis from the bottom
        /// </summary>
        /// <param name="data"></param>
        /// <param name="yMax"></param>
        public static void InvertAxis(IReadOnlyList<EntityAnnotation> data, int yMax)
        {
            // data = fillMissingValues(data);
            foreach (EntityAnnotation entityAnnotation in data)
            {
                foreach (Vertex vertex in entityAnnotation.BoundingPoly.Vertices)
                {
                    vertex.Y = yMax - vertex.Y;
                }
            }
        }

        public static void GetBoundingPolygon(List<ReceiptEntityAnnotation> mergedArray)
        {
            foreach (var item in mergedArray)
            {
                // calculate line height
                int h1 = Convert.ToInt32(item.Vertices[0].Y - item.Vertices[3].Y);
                int h2 = Convert.ToInt32(item.Vertices[1].Y - item.Vertices[2].Y);
                int h = h2 > h1 ? h2 : h1;
                double avgHeight = h * 0.6;

                VertexDouble[] arr1 = new VertexDouble[]
                {
          new VertexDouble(item.Vertices[1]),
          new VertexDouble(item.Vertices[0])
                };
                Line line1 = GetLine(arr1, false, avgHeight, true);

                VertexDouble[] arr2 = new VertexDouble[]
                {
          new VertexDouble(item.Vertices[2]),
          new VertexDouble(item.Vertices[3])
                };
                Line line2 = GetLine(arr2, false, avgHeight, false);

                item.BigBB = CreateRectCoordinates(line1, line2);
            }
        }

        static VertexDouble[] CreateRectCoordinates(Line line1, Line line2)
        {
            int? line1Ymin = Convert.ToInt32(line1.Ymin);
            int? line1Ymax = Convert.ToInt32(line1.Ymax);
            int? line2Ymax = Convert.ToInt32(line2.Ymax);
            int? line2Ymin = Convert.ToInt32(line2.Ymin);

            return new VertexDouble[]
            {
        new VertexDouble(line1.Xmin, line1Ymin),
        new VertexDouble(line1.Xmax, line1Ymax),
        new VertexDouble(line2.Xmax, line2Ymax),
        new VertexDouble(line2.Xmin, line2Ymin)
            };
        }

        static Line GetLine(VertexDouble[] v, bool isRoundValues, double avgHeight, bool isAdd)
        {
            if (isAdd)
            {
                v[1].Y = v[1].Y + avgHeight;
                v[0].Y = v[0].Y + avgHeight;
            }
            else
            {
                v[1].Y = v[1].Y - avgHeight;
                v[0].Y = v[0].Y - avgHeight;
            }

            double yDiff = v[1].Y - v[0].Y;
            double xDiff = v[1].X - v[0].X;

            double gradient = yDiff / xDiff;

            int xThreshMin = 1;
            int xThreshMax = 2000;

            double yMin;
            double yMax;
            if (gradient == 0)
            {
                // extend the line
                yMin = v[0].Y;
                yMax = v[0].Y;
            }
            else
            {
                yMin = v[0].Y - (gradient * (v[0].X - xThreshMin));
                yMax = v[0].Y + (gradient * (xThreshMax - v[0].X));
            }
            if (isRoundValues)
            {
                yMin = Math.Round(yMin);
                yMax = Math.Round(yMax);
            }
            return new Line { Xmin = xThreshMin, Xmax = xThreshMax, Ymin = yMin, Ymax = yMax };
        }

        internal static void CombineBoundingPolygon(List<ReceiptEntityAnnotation> mergedArray)
        {
            // select one word from the array
            for (int i = 0; i < mergedArray.Count; i++)
            {
                var bigBB = mergedArray[i].BigBB;

                // iterate through all the array to find the match
                for (int k = i; k < mergedArray.Count; k++)
                {
                    // Do not compare with the own bounding box and which was not matched with a line
                    if (k != i && mergedArray[k].Matched == false)
                    {
                        int insideCount = 0;
                        foreach (var coordinate in mergedArray[k].Vertices)
                        {
                            if (IsPointInRectangle(coordinate, bigBB))
                            {
                                ++insideCount;
                            }
                        }
                        // all four point were inside the big bb
                        if (insideCount == 4)
                        {
                            var match = new MatchLine { MatchCount = insideCount, MatchLineNum = k };
                            mergedArray[i].Match.Add(match);
                            mergedArray[k].Matched = true;
                        }
                    }
                }
            }
        }

        static bool IsPointInRectangle(Vertex coordinate, VertexDouble[] rectangle)
        {
            bool inside = false;
            for (int i = 0, j = 3; i < 4; j = i++)
            {
                if (((rectangle[i].Y <= coordinate.Y && coordinate.Y < rectangle[j].Y) || (rectangle[j].Y <= coordinate.Y && coordinate.Y < rectangle[i].Y)) &&
                   (coordinate.X < (rectangle[j].X - rectangle[i].X) * (coordinate.Y - rectangle[i].Y) / (rectangle[j].Y - rectangle[i].Y) + rectangle[i].X))
                    inside = !inside;
            }
            return inside;
        }
    }

    class Line
    {
        public int Xmin { get; set; }

        public int Xmax { get; set; }

        public double Ymin { get; set; }

        public double Ymax { get; set; }
    }

    internal class VertexDouble
    {
        public VertexDouble(int? x, int? y)
        {
            X = x.HasValue ? x.Value : default(int);
            Y = y.HasValue ? y.Value : default(int);
        }

        public VertexDouble(Vertex vertex) : this(vertex.X, vertex.Y) { }

        //
        // Summary:
        //     X coordinate.
        public double X { get; set; }

        //
        // Summary:
        //     Y coordinate.
        public double Y { get; set; }
    }
}
