// Allocator.h

#pragma once

using namespace System;

namespace Allocator {

	public ref class AlignedMalloc
	{
    public:
        static System::IntPtr GetPointer(int numberOfBytes); 
	};
}
