// This is the main DLL file.

#include "stdafx.h"

#include "Allocator.h"
#include <malloc.h>

using namespace Allocator; 

System::IntPtr AlignedMalloc::GetPointer(int numberOfBytes)
{
    void* ret = _aligned_malloc(numberOfBytes, 4096); 
    return System::IntPtr(ret);
}
