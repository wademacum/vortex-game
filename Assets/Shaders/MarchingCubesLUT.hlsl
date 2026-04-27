#ifndef MARCHING_CUBES_LUT_INCLUDED
#define MARCHING_CUBES_LUT_INCLUDED

static const int2 kTetraEdges[6] =
{
    int2(0, 1),
    int2(1, 2),
    int2(2, 0),
    int2(0, 3),
    int2(1, 3),
    int2(2, 3)
};

static const int4 kCubeTetrahedra[6] =
{
    int4(0, 5, 1, 6),
    int4(0, 1, 2, 6),
    int4(0, 2, 3, 6),
    int4(0, 3, 7, 6),
    int4(0, 7, 4, 6),
    int4(0, 4, 5, 6)
};

static const int3 kCornerOffset[8] =
{
    int3(0, 0, 0),
    int3(1, 0, 0),
    int3(1, 1, 0),
    int3(0, 1, 0),
    int3(0, 0, 1),
    int3(1, 0, 1),
    int3(1, 1, 1),
    int3(0, 1, 1)
};

#endif
