typedef struct _Point3D
{ 
    float X; 
    float Y; 
    float Z; 
} Point3D; 

kernel void Compute(
    global write_only int* neighbors,
    global read_only Point3D* points,
    int pointWidth,
    float radius)
{ 
    int i = get_global_id(0); 

    int iCounter = 0;
    float radiusSquared = radius * radius;
    for (int j = 0; j < pointWidth; j++)
    {
        float xDiff = fabs(points[j].X - points[i].X);
        float yDiff = fabs(points[j].Y - points[i].Y);
        float zDiff = fabs(points[j].Z - points[i].Z);
        if (j != i && xDiff < radius && yDiff < radius && zDiff < radius)
        {
            float magnitudeSquared = xDiff * xDiff + yDiff * yDiff + zDiff * zDiff;
            if (magnitudeSquared < radiusSquared)
            {
                neighbors[i*pointWidth+iCounter++] = j;
            }
        }
    }
    for(;iCounter<pointWidth;iCounter++)
    { 
        neighbors[i*pointWidth+iCounter] = -1;
    }
}

kernel void ComputeAsFloats(
    global write_only int* neighbors,
    global read_only float* points,
    int pointWidth,
    float radius)
{ 
    int i = get_global_id(0); 

    int iCounter = 0;
    float radiusSquared = radius * radius;
    for (int j = 0; j < pointWidth; j++)
    {
        float xDiff = fabs(points[j*3] - points[i*3]);
        float yDiff = fabs(points[j*3+1] - points[i*3+1]);
        float zDiff = fabs(points[j*3+2] - points[i*3+2]);
        if (j != i && xDiff < radius && yDiff < radius && zDiff < radius)
        {
            float magnitudeSquared = xDiff * xDiff + yDiff * yDiff + zDiff * zDiff;
            if (magnitudeSquared < radiusSquared)
            {
                neighbors[i*pointWidth+iCounter++] = j;
            }
        }
    }
    for(;iCounter<pointWidth;iCounter++)
    { 
        neighbors[i*pointWidth+iCounter] = -1;
    }
}
