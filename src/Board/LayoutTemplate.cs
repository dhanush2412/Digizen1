using System;
using System.Collections;
using System.Collections.Generic;

namespace VitaMahjongNumber.Board
{
    /// <summary>
    /// Defines the positions of all tiles in a level layout.
    /// Each position is encoded as a single int via TilePosition.EncodePos(x, y, z).
    ///
    /// Pure C# — no Unity dependencies. Safe for NUnit Edit Mode tests.
    ///
    /// Coordinate system:
    ///   x = column (increases right, must be >= 0 due to 5-bit mask)
    ///   y = row    (increases down)
    ///   z = layer  (0 = bottom, higher = stacked on top)
    ///   A tile at (x,y,z) COVERS (x,y,z-1) from above.
    /// </summary>
    public sealed class LayoutTemplate : IReadOnlyList<int>
    {
        private readonly int[] _slots;

        private LayoutTemplate(int[] slots)
        {
            _slots = slots;
        }

        /// <summary>Number of tiles in this layout (always even).</summary>
        public int TileCount => _slots.Length;

        // IReadOnlyList<int> implementation
        public int Count                          => _slots.Length;
        public int this[int index]                => _slots[index];
        public IEnumerator<int> GetEnumerator()   => ((IEnumerable<int>)_slots).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()   => _slots.GetEnumerator();

        // -----------------------------------------------------------------------
        // Factory Methods
        // -----------------------------------------------------------------------

        /// <summary>
        /// Standard 144-tile layout across 4 layers.
        /// Matches BuildClassicPool(8) = 144 tiles.
        ///
        /// Layer dimensions (x offset kept >= 0):
        ///   z=0: 12x5 = 60 tiles  (x=0..11, y=0..4)
        ///   z=1: 10x4 = 40 tiles  (x=1..10, y=0..3)
        ///   z=2:  8x4 = 32 tiles  (x=2..9,  y=0..3)
        ///   z=3:  3x4 = 12 tiles  (x=3..5,  y=0..3)
        ///   Total = 60 + 40 + 32 + 12 = 144
        /// </summary>
        public static LayoutTemplate CreateStandard144()
        {
            var seen  = new HashSet<int>(144);
            var slots = new List<int>(144);

            void Add(int x, int y, int z)
            {
                int pos = TilePosition.EncodePos(x, y, z);
                if (seen.Add(pos)) slots.Add(pos);
            }

            // z=0: 12 x 5
            for (int y = 0; y < 5; y++)
                for (int x = 0; x < 12; x++)
                    Add(x, y, 0);

            // z=1: 10 x 4
            for (int y = 0; y < 4; y++)
                for (int x = 1; x <= 10; x++)
                    Add(x, y, 1);

            // z=2: 8 x 4
            for (int y = 0; y < 4; y++)
                for (int x = 2; x <= 9; x++)
                    Add(x, y, 2);

            // z=3: 3 x 4
            for (int y = 0; y < 4; y++)
                for (int x = 3; x <= 5; x++)
                    Add(x, y, 3);

            return new LayoutTemplate(slots.ToArray());
        }

        /// <summary>
        /// Small 36-tile layout across 3 layers. Used for fast unit tests.
        /// Matches BuildClassicPool(2) = 36 tiles.
        ///
        ///   z=0: 4x5 = 20 tiles  (x=0..3, y=0..4)
        ///   z=1: 3x4 = 12 tiles  (x=0..2, y=0..3)
        ///   z=2: 2x2 =  4 tiles  (x=0..1, y=0..1)
        ///   Total = 20 + 12 + 4 = 36
        /// </summary>
        public static LayoutTemplate CreateSmall36()
        {
            var slots = new List<int>(36);

            // z=0: 4x5 = 20
            for (int y = 0; y < 5; y++)
                for (int x = 0; x < 4; x++)
                    slots.Add(TilePosition.EncodePos(x, y, 0));

            // z=1: 3x4 = 12
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 3; x++)
                    slots.Add(TilePosition.EncodePos(x, y, 1));

            // z=2: 2x2 = 4
            for (int y = 0; y < 2; y++)
                for (int x = 0; x < 2; x++)
                    slots.Add(TilePosition.EncodePos(x, y, 2));

            return new LayoutTemplate(slots.ToArray());
        }

        /// <summary>
        /// A layout that GUARANTEES the generator returns null.
        /// Contains exactly 1 tile — _freeTiles.Count = 1 is less than the required 2,
        /// so every retry returns null immediately, exhausting maxRetries.
        /// </summary>
        public static LayoutTemplate CreateImpossible()
        {
            return new LayoutTemplate(new int[]
            {
                TilePosition.EncodePos(0, 0, 0)
            });
        }
    }
}
