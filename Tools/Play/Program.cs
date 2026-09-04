using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ASTeams.SingleLine.Core;

namespace PlaySingleLine
{
    // Terminal front end for the Phase 1 rules core. Drives the real PathSession, so
    // whatever behaves correctly here will behave the same once the Unity view is
    // wired up in Phase 3.
    internal static class Program
    {
        private const string LevelsRelativePath =
            @"Assets\_Game\Asset_Resources\Level Data\oneline";

        /// <summary>
        /// Walks up from the executable until the project folder appears, so the tool
        /// runs from any working directory and from any checkout location.
        /// </summary>
        private static string FindLevelsRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, LevelsRelativePath);

                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Khong tim thay " + LevelsRelativePath);
        }

        private static readonly string Root = FindLevelsRoot();

        private static int[] ReadNumbers(string path)
        {
            return File.ReadAllText(path)
                .Split(new[] { ',', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToArray();
        }

        private static bool IsAscending(int[] values, int count)
        {
            for (int i = 1; i < count; i++)
            {
                if (values[i] <= values[i - 1])
                {
                    return false;
                }
            }

            return true;
        }

        private static LevelData Load(string pack, int number, out int[] solution)
        {
            string dir = Path.Combine(Root, pack);
            int[] raw = ReadNumbers(Path.Combine(dir, "Level_" + number + ".txt"));
            var grid = new Grid(raw[0], raw[1]);
            int[] payload = raw.Skip(2).ToArray();

            int[] cells;
            int start;

            if (IsAscending(payload, payload.Length))
            {
                cells = payload;
                start = LevelData.NoCell;
            }
            else if (IsAscending(payload, payload.Length - 1) &&
                     payload.Take(payload.Length - 1).Contains(payload[payload.Length - 1]))
            {
                cells = payload.Take(payload.Length - 1).ToArray();
                start = payload[payload.Length - 1];
            }
            else
            {
                cells = payload;
                start = payload[0];
            }

            string tutorial = Path.Combine(dir, "Toturial_" + number + ".txt");
            solution = File.Exists(tutorial) ? ReadNumbers(tutorial) : null;

            if (solution != null && solution.Length > 0)
            {
                start = solution[0];
            }

            return new LevelData(pack + "/" + number, 1, grid, cells, fixedStart: start, solution: solution);
        }

        private static void Draw(PathSession session, string message)
        {
            LevelData level = session.Level;
            Grid grid = level.Grid;

            var order = new Dictionary<int, int>();

            for (int step = 0; step < session.Length; step++)
            {
                order[session.GetCell(step)] = step + 1;
            }

            // Clear throws when stdout is redirected, which is fine: the frames just
            // scroll instead of repainting in place.
            try { Console.Clear(); } catch (IOException) { }
            Console.WriteLine("  " + level.Id + "   " + grid + "   " +
                              session.Length + "/" + level.ActiveCellCount + " o");
            Console.WriteLine();

            for (int row = 0; row < grid.Height; row++)
            {
                Console.Write("   ");

                for (int column = 0; column < grid.Width; column++)
                {
                    int cell = row * grid.Width + column;

                    if (!level.IsActive(cell))
                    {
                        Console.Write("     ");
                        continue;
                    }

                    if (cell == session.Head)
                    {
                        Write(ConsoleColor.Black, ConsoleColor.Yellow, "[" + order[cell].ToString("00") + "]");
                    }
                    else if (order.TryGetValue(cell, out int step))
                    {
                        Write(ConsoleColor.Black, ConsoleColor.DarkCyan, " " + step.ToString("00") + " ");
                    }
                    else if (cell == level.FixedStart)
                    {
                        Write(ConsoleColor.White, ConsoleColor.DarkRed, " () ");
                    }
                    else
                    {
                        Write(ConsoleColor.Gray, ConsoleColor.DarkGray, " .. ");
                    }

                    Console.Write(" ");
                }

                Console.WriteLine();
                Console.WriteLine();
            }

            Console.Write("   trang thai: ");
            Console.ForegroundColor = session.State switch
            {
                PathState.Won => ConsoleColor.Green,
                PathState.Stuck => ConsoleColor.Red,
                _ => ConsoleColor.White
            };
            Console.Write(session.State);
            Console.ResetColor();
            Console.WriteLine("      " + message);
            Console.WriteLine();
            Console.WriteLine("   mui ten hoac WASD di chuyen | U undo | R choi lai | H goi y | Q thoat");
        }

        private static void Write(ConsoleColor fore, ConsoleColor back, string text)
        {
            Console.ForegroundColor = fore;
            Console.BackgroundColor = back;
            Console.Write(text);
            Console.ResetColor();
        }

        private static void Main(string[] args)
        {
            string pack = args.Length > 0 ? args[0] : "medium";
            int number = args.Length > 1 ? int.Parse(args[1]) : 442;
            bool autoPlay = args.Contains("--auto");
            int movesIndex = Array.IndexOf(args, "--moves");
            string moves = movesIndex >= 0 && movesIndex + 1 < args.Length ? args[movesIndex + 1] : null;

            LevelData level = Load(pack, number, out int[] solution);
            var session = new PathSession(level);

            if (level.HasFixedStart)
            {
                session.Move(level.FixedStart);
            }

            if (autoPlay)
            {
                RunAuto(session, solution);
                return;
            }

            if (moves != null)
            {
                RunMoves(session, solution, moves);
                return;
            }

            if (Console.IsInputRedirected)
            {
                Console.WriteLine("Terminal nay khong nhan phim (stdin bi redirect).");
                Console.WriteLine("Dung mot trong hai cach:");
                Console.WriteLine("  1. Them --moves \"wasd...\" de gui ca chuoi nuoc di mot lan");
                Console.WriteLine("  2. Mo PowerShell rieng roi chay lai lenh nay de choi bang phim");
                return;
            }

            string message = string.Empty;

            while (true)
            {
                Draw(session, message);
                message = string.Empty;

                ConsoleKey key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.Q)
                {
                    return;
                }

                if (key == ConsoleKey.R)
                {
                    session.Restart();
                    session.Move(level.FixedStart);
                    continue;
                }

                if (key == ConsoleKey.U)
                {
                    if (!session.Undo())
                    {
                        message = "khong undo duoc";
                    }

                    continue;
                }

                if (key == ConsoleKey.H)
                {
                    message = Hint(session, solution);
                    continue;
                }

                if (!TryGetDirection(key, out Direction direction))
                {
                    continue;
                }

                if (!level.Grid.TryGetNeighbor(session.Head, direction, out int target))
                {
                    message = "cham mep bang";
                    continue;
                }

                if (session.Move(target) == MoveResult.Rejected)
                {
                    message = "nuoc di khong hop le";
                }
            }
        }

        private static bool TryGetDirection(ConsoleKey key, out Direction direction)
        {
            switch (key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.W:
                    direction = Direction.Up;
                    return true;

                case ConsoleKey.RightArrow:
                case ConsoleKey.D:
                    direction = Direction.Right;
                    return true;

                case ConsoleKey.DownArrow:
                case ConsoleKey.S:
                    direction = Direction.Down;
                    return true;

                case ConsoleKey.LeftArrow:
                case ConsoleKey.A:
                    direction = Direction.Left;
                    return true;

                default:
                    direction = Direction.Up;
                    return false;
            }
        }

        /// <summary>Names the next cell of the shipped solution, when the path is still on it.</summary>
        private static string Hint(PathSession session, int[] solution)
        {
            if (solution == null || session.Length >= solution.Length)
            {
                return "khong co goi y";
            }

            for (int step = 0; step < session.Length; step++)
            {
                if (session.GetCell(step) != solution[step])
                {
                    return "duong di da lech khoi loi giai mau, thu undo";
                }
            }

            return "goi y: di toi o " + solution[session.Length];
        }

        /// <summary>
        /// Applies a whole string of moves at once, so the level can be played from a
        /// terminal that cannot deliver individual key presses.
        /// </summary>
        private static void RunMoves(PathSession session, int[] solution, string moves)
        {
            var log = new List<string>();

            foreach (char raw in moves)
            {
                char key = char.ToLowerInvariant(raw);

                if (key == ' ' || key == ',' || key == '-')
                {
                    continue;
                }

                if (key == 'u')
                {
                    log.Add(session.Undo() ? "u undo" : "u KHONG UNDO DUOC");
                    continue;
                }

                if (key == 'r')
                {
                    session.Restart();
                    session.Move(session.Level.FixedStart);
                    log.Add("r choi lai");
                    continue;
                }

                if (key == 'h')
                {
                    log.Add("h " + Hint(session, solution));
                    continue;
                }

                Direction direction;

                switch (key)
                {
                    case 'w': direction = Direction.Up; break;
                    case 'd': direction = Direction.Right; break;
                    case 's': direction = Direction.Down; break;
                    case 'a': direction = Direction.Left; break;
                    default:
                        log.Add(raw + " phim la, bo qua");
                        continue;
                }

                if (!session.Level.Grid.TryGetNeighbor(session.Head, direction, out int target))
                {
                    log.Add(key + " cham mep bang");
                    continue;
                }

                MoveResult result = session.Move(target);

                if (result == MoveResult.Rejected)
                {
                    log.Add(key + " -> o " + target + " BI TU CHOI");
                }
                else if (result != MoveResult.Moved)
                {
                    log.Add(key + " -> o " + target + " (" + result + ")");
                }
            }

            Draw(session, "da xu ly " + moves.Length + " phim");

            if (log.Count == 0)
            {
                return;
            }

            Console.WriteLine();
            Console.WriteLine("   nhat ky:");

            foreach (string line in log)
            {
                Console.WriteLine("     " + line);
            }
        }

        /// <summary>Replays the shipped solution, for terminals without key input.</summary>
        private static void RunAuto(PathSession session, int[] solution)
        {
            if (solution == null)
            {
                Console.WriteLine("Man nay khong co file Toturial_ nen khong co loi giai mau.");
                return;
            }

            for (int step = 1; step < solution.Length; step++)
            {
                MoveResult result = session.Move(solution[step]);
                Draw(session, "auto: buoc " + (step + 1) + " -> o " + solution[step] + " (" + result + ")");
                System.Threading.Thread.Sleep(220);
            }

            Console.WriteLine();
            Console.WriteLine(session.State == PathState.Won ? "   THANG" : "   chua thang");
        }
    }
}
