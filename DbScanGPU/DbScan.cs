using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbScanGPU
{
    public static class DbScan
    {
        public static int[][] GetNeighbors(Stopwatch sw, Point3D[] points, double radius)
        {
            if(sw!=null)
            {
                sw.Start();
            }
            int[][] neighbors = new int[points.Length][];
            for (int i = 0; i < points.Length; i++)
            {
                neighbors[i] = new int[points.Length];
                for (int j = 0; j < points.Length; j++)
                {
                    neighbors[i][j] = -1;
                }
            }

            double radiusSquared = radius * radius;

            Parallel.For(0, points.Length, i =>
            {
                int iCounter = 0;
                for (int j = 0; j < points.Length; j++)
                {
                    float xDiff = Math.Abs(points[j].X - points[i].X);
                    float yDiff = Math.Abs(points[j].Y - points[i].Y);
                    float zDiff = Math.Abs(points[j].Z - points[i].Z);
                    if (j != i && xDiff < radius && yDiff < radius && zDiff < radius)
                    {
                        float magnitudeSquared = xDiff * xDiff + yDiff * yDiff + zDiff * zDiff;
                        if (magnitudeSquared < radiusSquared)
                        {
                            neighbors[i][iCounter++] = j;
                        }
                    }
                }
            });

            if(sw != null)
            {
                sw.Stop();
            }

            return neighbors;
        }
    }
}
