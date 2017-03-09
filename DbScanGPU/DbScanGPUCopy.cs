using Cloo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DbScanGPU
{
    public static class DbScanGPUCopy
    {
        private static ComputePlatform _platform;
        private static ComputeContext _context;
        private static ComputeCommandQueue _queue;
        private static ComputeProgram _program;
        private static ComputeKernel _kernel;

        public static void Initialize(int numberOfPoints)
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
            }
        }

        public static int[] GetNeighbors(Stopwatch sw, Point3D[] points, double radius)
        {
            if(sw != null)
            {
                sw.Start(); 
            }

            int[] ret = new int[points.Length * points.Length]; 
            ComputeBuffer<int> neighbors = new ComputeBuffer<int>(
                _context,
                ComputeMemoryFlags.WriteOnly | ComputeMemoryFlags.CopyHostPointer,
                ret);
            _kernel.SetMemoryArgument(0, neighbors);

            ComputeBuffer<Point3D> point3DBuffer = new ComputeBuffer<Point3D>(
                _context,
                ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer,
                points);
            _kernel.SetMemoryArgument(1, point3DBuffer);

            _kernel.SetValueArgument<int>(2, points.Length);
            _kernel.SetValueArgument<float>(3, (float)radius);

            _queue.Execute(_kernel,
                new long[0],
                new long[] { points.Length },
                null, null);

            unsafe
            {
                fixed (int* retPtr = ret)
                {
                    _queue.Read(neighbors, false, 0, ret.Length, new IntPtr(retPtr), null);
                }
            }
            _queue.Finish(); 

            if(sw!=null)
            {
                sw.Stop(); 
            }

            return ret;
        }
    }
}
