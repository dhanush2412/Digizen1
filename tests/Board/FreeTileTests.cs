using System.Collections.Generic;
using NUnit.Framework;
using VitaMahjongNumber.Board;

namespace VitaMahjongNumber.Tests.Board
{
    /// <summary>
    /// Unit tests for FreeTileChecker.IsFree (delegates to TilePosition.IsFree).
    /// Tests all 10 neighbour combinations + boundary/regression cases.
    /// </summary>
    [TestFixture]
    public class FreeTileTests
    {
        // Tile under test: always at (5, 5, 1) unless noted
        private const int TX = 5, TY = 5, TZ = 1;

        private static HashSet<int> Occupied(params (int x, int y, int z)[] positions)
        {
            var set = new HashSet<int>();
            foreach (var (x, y, z) in positions)
                set.Add(TilePosition.EncodePos(x, y, z));
            return set;
        }

        // Test 1: Completely isolated — no neighbours
        [Test]
        public void IsFree_IsolatedTile_ReturnsTrue()
        {
            var occupied = Occupied((TX, TY, TZ));
            Assert.IsTrue(FreeTileChecker.IsFree(TX, TY, TZ, occupied));
        }

        // Test 2: Tile covered directly above (z+1)
        [Test]
        public void IsFree_CoveredAbove_ReturnsFalse()
        {
            var occupied = Occupied((TX, TY, TZ), (TX, TY, TZ + 1));
            Assert.IsFalse(FreeTileChecker.IsFree(TX, TY, TZ, occupied));
        }

        // Test 3: Blocked left only — right is open -> FREE
        [Test]
        public void IsFree_BlockedLeftOnly_ReturnsTrue()
        {
            var occupied = Occupied((TX, TY, TZ), (TX - 1, TY, TZ));
            Assert.IsTrue(FreeTileChecker.IsFree(TX, TY, TZ, occupied),
                "Blocked on left but open on right — should be free");
        }

        // Test 4: Blocked right only — left is open -> FREE
        [Test]
        public void IsFree_BlockedRightOnly_ReturnsTrue()
        {
            var occupied = Occupied((TX, TY, TZ), (TX + 1, TY, TZ));
            Assert.IsTrue(FreeTileChecker.IsFree(TX, TY, TZ, occupied),
                "Blocked on right but open on left — should be free");
        }

        // Test 5: Blocked both sides, no cover -> BLOCKED
        [Test]
        public void IsFree_BlockedBothSides_ReturnsFalse()
        {
            var occupied = Occupied((TX, TY, TZ), (TX - 1, TY, TZ), (TX + 1, TY, TZ));
            Assert.IsFalse(FreeTileChecker.IsFree(TX, TY, TZ, occupied),
                "Blocked on both sides — cannot slide out");
        }

        // Test 6: Covered above + blocked left -> BLOCKED (cover alone suffices)
        [Test]
        public void IsFree_CoveredAndBlockedLeft_ReturnsFalse()
        {
            var occupied = Occupied((TX, TY, TZ), (TX, TY, TZ + 1), (TX - 1, TY, TZ));
            Assert.IsFalse(FreeTileChecker.IsFree(TX, TY, TZ, occupied));
        }

        // Test 7: Covered above + blocked both sides -> BLOCKED
        [Test]
        public void IsFree_CoveredAndBlockedBothSides_ReturnsFalse()
        {
            var occupied = Occupied((TX, TY, TZ), (TX, TY, TZ + 1), (TX - 1, TY, TZ), (TX + 1, TY, TZ));
            Assert.IsFalse(FreeTileChecker.IsFree(TX, TY, TZ, occupied));
        }

        // Test 8: Neighbour in different row (y+1) same z — should NOT block
        [Test]
        public void IsFree_NeighbourDifferentRow_ReturnsTrue()
        {
            var occupied = Occupied((TX, TY, TZ), (TX, TY + 1, TZ));
            Assert.IsTrue(FreeTileChecker.IsFree(TX, TY, TZ, occupied),
                "y-axis neighbours at same z should not affect horizontal freedom");
        }

        // Test 9 (regression): Tile covered at z+2 but NOT z+1 -> FREE
        [Test]
        public void IsFree_CoveredAtZ2NotZ1_ReturnsTrue()
        {
            var occupied = Occupied((TX, TY, TZ), (TX, TY, TZ + 2));
            Assert.IsTrue(FreeTileChecker.IsFree(TX, TY, TZ, occupied),
                "Only direct cover at z+1 counts — z+2 should not block");
        }

        // Test 10: Single ground tile on empty board -> FREE
        [Test]
        public void IsFree_SingleGroundTile_ReturnsTrue()
        {
            var occupied = Occupied((3, 3, 0));
            Assert.IsTrue(FreeTileChecker.IsFree(3, 3, 0, occupied));
        }

        // Test 11 (anti-regression): Verify correct formula direction
        // !blockedLeft || !blockedRight (correct) vs blockedLeft || blockedRight (bug)
        [Test]
        public void IsFree_FormulaDirection_NotInverted()
        {
            var occupiedLeft = Occupied((TX, TY, TZ), (TX - 1, TY, TZ));
            Assert.IsTrue(FreeTileChecker.IsFree(TX, TY, TZ, occupiedLeft),
                "FORMULA BUG: condition must be !blockedLeft || !blockedRight, not blockedLeft || blockedRight");

            var occupiedRight = Occupied((TX, TY, TZ), (TX + 1, TY, TZ));
            Assert.IsTrue(FreeTileChecker.IsFree(TX, TY, TZ, occupiedRight),
                "FORMULA BUG: blocked on right only must still be free");
        }
    }
}
