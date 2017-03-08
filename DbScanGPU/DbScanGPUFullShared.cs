using Cloo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DbScanGPU
{
    public static class DbScanGPUFullShared
    {
        private static ComputePlatform _platform;
        private static ComputeContext _context;
        private static ComputeCommandQueue _queue;
        private static ComputeProgram _program;
        private static ComputeKernel _kernel;
        private static unsafe int* _preBuffer;
        private static unsafe Point3D* _pointBuffer;
        private static ComputeBuffer<int> _neighbors;
        private static ComputeBuffer<Point3D> _point3DBuffer;

        public static unsafe Point3D* Initialize(int numberOfPoints)
        {
            if (_platform == null)
            {
                _platform = ComputePlatform.Platforms.Where(n => n.Name.Contains("Intel")).First();
                _context = new ComputeContext(
                    ComputeDeviceTypes.Gpu,
                    new ComputeContextPropertyList(_platform),
                    null, IntPtr.Zero);
                _queue = new ComputeCommandQueue(
                    _context,
                    _context.Devices[0],
                    ComputeCommandQueueFlags.None);
                string source = null;
                using (StreamReader sr = new StreamReader("kernel.cl"))
                {
                    source = sr.ReadToEnd();
                }
                _program = new ComputeProgram(_context, new string[] { source });
                _program.Build(null, null, null, IntPtr.Zero);
                _kernel = _program.CreateKernel("Compute");

                int numberOfIntsInAPage = 4096 / sizeof(int);
                int numberOfInts = numberOfPoints * numberOfPoints;
                numberOfInts = numberOfInts + (numberOfInts % numberOfIntsInAPage);

                int numberOfPointsInAPage = 4096 / sizeof(Point3D);
                numberOfPoints = numberOfPoints + (numberOfPoints % numberOfPointsInAPage);

                unsafe
                {
                    _preBuffer = (int*)Allocator.AlignedMalloc.GetPointer(
                        numberOfInts * sizeof(int)).ToPointer();
                    _pointBuffer = (Point3D*)Allocator.AlignedMalloc.GetPointer(
                        numberOfPoints * sizeof(Point3D)).ToPointer();
                }

                unsafe
                {
                    _neighbors = new ComputeBuffer<int>(
                        _context,
                        ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer,
                        numberOfInts,
                        new IntPtr((void*)_preBuffer));

                    _point3DBuffer = new ComputeBuffer<Point3D>(
                        _context,
                        ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.UseHostPointer,
                        numberOfPoints,
                        new IntPtr((void*)_pointBuffer));
                }


                _kernel.SetMemoryArgument(1, _point3DBuffer);
                _kernel.SetMemoryArgument(0, _neighbors);
            }
            return _pointBuffer;
        }

        public static unsafe int* GetNeighbors(Point3D[] points, int pointsLength, double radius)
        {
            //Console.WriteLine($"{_pointBuffer[10].X}, {_pointBuffer[10].Y}, {_pointBuffer[10].Z}");

            IntPtr mappedPtr2 = _queue.Map(_point3DBuffer,
                true, ComputeMemoryMappingFlags.Write,
                0, pointsLength, null);
            for(int c=0;c<points.Length;c++)
            {
                _pointBuffer[c] = points[c]; 
            }
            _queue.Unmap(_point3DBuffer, ref mappedPtr2, null);

            //Console.WriteLine($"{_pointBuffer[10].X}, {_pointBuffer[10].Y}, {_pointBuffer[10].Z}"); 

            _kernel.SetValueArgument<int>(2, pointsLength);
            _kernel.SetValueArgument<float>(3, (float)radius);

            _queue.Execute(_kernel,
                new long[0],
                new long[] { pointsLength },
                null, null);

            IntPtr mappedPtr = _queue.Map(_neighbors,
                true, ComputeMemoryMappingFlags.Read,
                0, pointsLength * pointsLength, null);
            _queue.Unmap(_neighbors, ref mappedPtr, null);

            return _preBuffer;
        }
    }
}
