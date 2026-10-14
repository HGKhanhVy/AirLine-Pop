using NUnit.Framework;

namespace AirlinePop.SingleLine.Core.Tests
{
    public sealed class PassengerSeatTests
    {
        [Test]
        public void CatsNeverSitOnASquareThatCarriesARule()
        {
            // 4 x 4, every square open, runway at 15, wind at 5, a cloud at 10.
            var cells = new int[16];

            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = i;
            }

            var level = new LevelData("seats", 1, new Grid(4, 4), cells, 0, 15, wind: new[] { new WindCell(5, Direction.Right) },
                marks: new[] { new CellMark(10, CellKind.Straight) });

            for (int attempt = 0; attempt < 20; attempt++)
            {
                LevelData renamed = level.WithIdentity("seats" + attempt, 1);

                foreach (int seat in PassengerPlanner.Plan(renamed).Cells)
                {
                    Assert.AreNotEqual(15, seat);
                    Assert.AreNotEqual(5, seat);
                    Assert.AreNotEqual(10, seat);
                }
            }
        }
    }
}
