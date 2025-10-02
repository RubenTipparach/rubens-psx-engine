using System;

namespace rubens_psx_engine.system.procedural
{
    /// <summary>
    /// Stores neighbor LOD information for a chunk
    /// </summary>
    public class ChunkNeighborInfo
    {
        public int LeftNeighborLOD { get; set; } = -1;
        public int RightNeighborLOD { get; set; } = -1;
        public int BottomNeighborLOD { get; set; } = -1;
        public int TopNeighborLOD { get; set; } = -1;
    }
}
