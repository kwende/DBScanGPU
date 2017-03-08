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
            for(int c=0;c<lines.Length;c++)
            {
                string line = lines[c];
                string[] bits = line.Split(',');
                ret[c].X = float.Parse(bits[0]);
                ret[c].Y = float.Parse(bits[1]);
                ret[c].Z = float.Parse(bits[2]);
            }

            return ret; 
        }

        static void Main(string[] args)
        {
            const int NumberOfIterations = 100;
            Point3D[] randomlyDistributedPoints = getPointsFromFile("testpoints.csv"); 
            //int NumberOfPixels = 1000; // int.Parse(args[0]);

            //Point3D[] randomlyDistributedPoints = getRandomNormallyDistributedPoints(NumberOfPixels);
            int NumberOfPixels = randomlyDistributedPoints.Length;
            // NOTE: gpuRet is the SAME array each time, it's just overwritten. 
            // this means it is NOT thread safe. 

            unsafe
            {
                DbScanGPUFullShared.Initialize(randomlyDistributedPoints.Length);
                DbScanGPUCopy.Initialize(randomlyDistributedPoints.Length);
                DbScanGPUShared.Initialize(randomlyDistributedPoints.Length); 

                int* gpuRet = DbScanGPUFullShared.GetNeighbors(randomlyDistributedPoints, randomlyDistributedPoints.Length, 100);
                int[] gpuRet2 = DbScanGPUCopy.GetNeighbors(randomlyDistributedPoints, 100);
                int* gpuRet3 = DbScanGPUShared.GetNeighbors(randomlyDistributedPoints, randomlyDistributedPoints.Length, 100);
                int[][] cpuRet = DbScan.GetNeighbors(randomlyDistributedPoints, 100);

                for (int y = 0, i = 0; y < randomlyDistributedPoints.Length; y++)
                {
                    for (int x = 0; x < randomlyDistributedPoints.Length; x++, i++)
                    {
                        if (gpuRet[i] != cpuRet[y][x] || gpuRet2[i] != cpuRet[y][x] || gpuRet3[i] != cpuRet[y][x])
                        {
                            throw new Exception(
                                $"Expected {cpuRet[y][x]}, got {gpuRet[i]}");
                        }
                    }
                }
            }


            Stopwatch fullSharedTime = new Stopwatch();
            fullSharedTime.Start();
            unsafe
            {
                for (int c = 0; c < NumberOfIterations; c++)
                {
                    DbScanGPUFullShared.GetNeighbors(randomlyDistributedPoints, randomlyDistributedPoints.Length, 100);
                }
            }
            fullSharedTime.Stop();

            Stopwatch cpuTime = new Stopwatch();
            cpuTime.Start();
            for (int c = 0; c < NumberOfIterations; c++)
            {
                DbScan.GetNeighbors(randomlyDistributedPoints, 100);
            }
            cpuTime.Stop();

            Stopwatch copyTime = new Stopwatch();
            copyTime.Start();
            for (int c = 0; c < NumberOfIterations; c++)
            {
                DbScanGPUCopy.GetNeighbors(randomlyDistributedPoints, 100);
            }
            copyTime.Stop();

            Stopwatch readSharedTime = new Stopwatch();
            readSharedTime.Start();
            unsafe
            {
                for (int c = 0; c < NumberOfIterations; c++)
                {
                    DbScanGPUShared.GetNeighbors(randomlyDistributedPoints, randomlyDistributedPoints.Length, 100);
                }
            }
            readSharedTime.Stop();

            Console.WriteLine($"# of points {NumberOfPixels}");
            Console.WriteLine("GPU Read Shared: " + readSharedTime.ElapsedMilliseconds / (NumberOfIterations * 1.0f));
            Console.WriteLine("GPU Full Shared: " + fullSharedTime.ElapsedMilliseconds / (NumberOfIterations * 1.0f));
            Console.WriteLine("GPU Copy: " + copyTime.ElapsedMilliseconds / (NumberOfIterations * 1.0f));
            Console.WriteLine("CPU: " + cpuTime.ElapsedMilliseconds / (NumberOfIterations * 1.0f));
            Console.WriteLine("========================="); 
            Console.WriteLine($"GPU shared is {cpuTime.ElapsedMilliseconds / (readSharedTime.ElapsedMilliseconds * 1.0f)}x faster than CPU.");
            Console.WriteLine($"GPU full shared is {cpuTime.ElapsedMilliseconds / (fullSharedTime.ElapsedMilliseconds * 1.0f)}x faster than CPU.");
            Console.WriteLine($"GPU copy is {cpuTime.ElapsedMilliseconds / (copyTime.ElapsedMilliseconds * 1.0f)}x faster");

            return;
        }
    }
}
