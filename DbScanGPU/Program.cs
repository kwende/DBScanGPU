using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbScanGPU
{
    class Program
    {
        static float getRandomNormallyDistributedValue(Random rand, float mean, float stdDev)
        {
            float u1 = 1.0f - (float)rand.NextDouble();
            float u2 = 1.0f - (float)rand.NextDouble();
            float randStdNormal = (float)(Math.Sqrt(-2.0 * Math.Log(u1)) *
                         Math.Sin(2.0 * Math.PI * u2));
            float randNormal =
                         mean + stdDev * randStdNormal;

            return randNormal;
        }

        static Point3D[] getRandomNormallyDistributedPoints(int numberOfPoints)
        {
            Random rand = new Random();

            Point3D[] ret = new Point3D[numberOfPoints];

            for (int c = 0; c < numberOfPoints; c++)
            {
                Point3D point = new Point3D();
                point.X = getRandomNormallyDistributedValue(rand, 0, 100.0f);
                point.Y = getRandomNormallyDistributedValue(rand, 0, 100.0f);
                point.Z = getRandomNormallyDistributedValue(rand, 0, 100.0f);

                ret[c] = point;
            }

            return ret;
        }

        static Point3D[] getPointsFromFile(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);

            Point3D[] ret = new Point3D[lines.Length];
            for (int c = 0; c < lines.Length; c++)
            {
                string line = lines[c];
                string[] bits = line.Split(',');
                ret[c].X = float.Parse(bits[0]);
                ret[c].Y = float.Parse(bits[1]);
                ret[c].Z = float.Parse(bits[2]);
            }

            return ret;
        }

        static Point3D[] ShufflePoints(Point3D[] allPoints)
        {
            Random rand = new Random();

            Point3D[] ret = allPoints.OrderBy(n => rand.Next()).Take(rand.Next(800, allPoints.Length)).ToArray();
            for (int c = 0; c < ret.Length; c++)
            {
                ret[c].X += rand.Next(-10, 10);
                ret[c].Y += rand.Next(-10, 10);
                ret[c].Z += rand.Next(-10, 10);
            }
            return ret;
        }

        static void Main(string[] args)
        {
            const int MaximumNumberOfPoints = 2000;
            List<Point3D[]> allPoints = new List<Point3D[]>();
            Console.Write("Reading the frames..."); 
            foreach (string file in Directory.GetFiles("Points", "*.csv"))
            {
                allPoints.Add(getPointsFromFile("testpoints.csv"));
            }
            Console.WriteLine("...done");

            Console.Write("Testing the points..."); 
            unsafe
            {
                DbScanGPUFullShared.Initialize(MaximumNumberOfPoints);
                DbScanGPUCopy.Initialize(MaximumNumberOfPoints);
                DbScanGPUShared.Initialize(MaximumNumberOfPoints);

                // uncomment to test. 
                int* gpuRet = DbScanGPUFullShared.GetNeighbors(null, allPoints[0], allPoints[0].Length, 100);
                int[] gpuRet2 = DbScanGPUCopy.GetNeighbors(null, allPoints[0], 100);
                int* gpuRet3 = DbScanGPUShared.GetNeighbors(null, allPoints[0], allPoints[0].Length, 100);
                int[][] cpuRet = DbScan.GetNeighbors(null, allPoints[0], 100);

                for (int y = 0, i = 0; y < allPoints[0].Length; y++)
                {
                    for (int x = 0; x < allPoints[0].Length; x++, i++)
                    {
                        if (gpuRet[i] != cpuRet[y][x] || gpuRet2[i] != cpuRet[y][x] || gpuRet3[i] != cpuRet[y][x])
                        {
                            throw new Exception(
                                $"Expected {cpuRet[y][x]}, got {gpuRet[i]}");
                        }
                    }
                }
            }
            Console.WriteLine("...succeded.");


            Console.Write("Testing performance..."); 
            int numberOfFrames = allPoints.Count;

            Random rand = new Random();
            Stopwatch fullSharedTime = new Stopwatch();
            unsafe
            {
                for (int c = 0; c < numberOfFrames; c++)
                {
                    Point3D[] pointsToInspect = allPoints[c]; 
                    DbScanGPUFullShared.GetNeighbors(fullSharedTime, pointsToInspect, pointsToInspect.Length, 100);
                }
            }

            Stopwatch cpuTime = new Stopwatch();
            for (int c = 0; c < numberOfFrames; c++)
            {
                Point3D[] pointsToInspect = allPoints[c];
                DbScan.GetNeighbors(cpuTime, pointsToInspect, 100);
            }

            Stopwatch copyTime = new Stopwatch();
            for (int c = 0; c < numberOfFrames; c++)
            {
                Point3D[] pointsToInspect = allPoints[c];
                DbScanGPUCopy.GetNeighbors(copyTime, pointsToInspect, 100);
            }

            Stopwatch readSharedTime = new Stopwatch();
            unsafe
            {
                for (int c = 0; c < numberOfFrames; c++)
                {
                    Point3D[] pointsToInspect = allPoints[c];
                    DbScanGPUShared.GetNeighbors(readSharedTime, pointsToInspect, pointsToInspect.Length, 100);
                }
            }
            Console.WriteLine("...done"); 

            Console.WriteLine($"# of frames {numberOfFrames}");
            Console.WriteLine("GPU Read Shared: " + readSharedTime.ElapsedMilliseconds / (numberOfFrames * 1.0f));
            Console.WriteLine("GPU Full Shared: " + fullSharedTime.ElapsedMilliseconds / (numberOfFrames * 1.0f));
            Console.WriteLine("GPU Copy: " + copyTime.ElapsedMilliseconds / (numberOfFrames * 1.0f));
            Console.WriteLine("CPU: " + cpuTime.ElapsedMilliseconds / (numberOfFrames * 1.0f));
            Console.WriteLine("=========================");
            Console.WriteLine($"GPU shared is {cpuTime.ElapsedMilliseconds / (readSharedTime.ElapsedMilliseconds * 1.0f)}x faster than CPU.");
            Console.WriteLine($"GPU full shared is {cpuTime.ElapsedMilliseconds / (fullSharedTime.ElapsedMilliseconds * 1.0f)}x faster than CPU.");
            Console.WriteLine($"GPU copy is {cpuTime.ElapsedMilliseconds / (copyTime.ElapsedMilliseconds * 1.0f)}x faster");

            return;
        }
    }
}
