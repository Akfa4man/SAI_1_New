using System.Globalization;

namespace SAI_1_Console_1_ver2
{
    public enum UiRole
    {
        Program,
        Player,
        Menu,
        Notice,
        Warning
    }

    public class ConsoleIO
    {
        public void Print(string text = "", UiRole role = UiRole.Program, bool newline = true, bool withLabel = true)
        {
            var (fg, bg) = ColorsFor(role);
            var oldFg = Console.ForegroundColor;
            var oldBg = Console.BackgroundColor;
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;

            bool suppressLabel = newline && string.IsNullOrEmpty(text);
            var prefix = (withLabel && !suppressLabel) ? LabelFor(role) : "";

            if (newline) Console.WriteLine(prefix + text);
            else Console.Write(prefix + text);

            Console.ForegroundColor = oldFg;
            Console.BackgroundColor = oldBg;
        }

        public void PrintWelcome()
        {
            Print("Добро пожаловать в игру «Угадай хобби».", UiRole.Program);
            Print();
        }

        public void PrintMenuQueries()
        {
            Print("Возможные запросы:", UiRole.Menu);
            Print("   Давай сыграем.", UiRole.Menu, withLabel: false);
            Print("   Я хочу узнать, что ты знаешь об одном из хобби.", UiRole.Menu, withLabel: false);
            Print("   Выведи всю базу знаний на экран.", UiRole.Menu, withLabel: false);
            Print("   Я хочу узнать, известно ли тебе о нужном мне хобби.", UiRole.Menu, withLabel: false);
            Print("   Выход.", UiRole.Menu, withLabel: false);
            Print();
        }

        public void WaitAnyKey(string prompt = "Нажмите любую клавишу…")
        {
            Print(prompt, UiRole.Notice);
            Console.ReadKey(true);
        }

        public string AskLine(string programPrompt)
        {
            Print(programPrompt, UiRole.Program);
            return ReadLineAsRole(UiRole.Player, withLabel: true);
        }

        public string AskNonEmptyLine(string programPrompt)
        {
            while (true)
            {
                Print(programPrompt, UiRole.Program);
                var s = ReadLineAsRole(UiRole.Player, withLabel: true);
                if (!string.IsNullOrWhiteSpace(s)) return s;
                Print("Похоже, что вы ввели пустую строку. Повторите ввод.", UiRole.Warning);
            }
        }

        public bool AskYesNo(string prompt)
        {
            prompt = NormalizeQuestion(prompt);

            while (true)
            {
                Print($"{prompt} (да/нет):", UiRole.Program);
                var s = (ReadLineAsRole(UiRole.Player, withLabel: true) ?? "")
                          .Trim().ToLowerInvariant();

                if (s.Length == 0)
                {
                    Print("Похоже, что вы ввели пустую строку. Повторите ввод.", UiRole.Warning);
                    continue;
                }
                if (s == "да" || s == "д" || s == "y" || s == "yes") return true;
                if (s == "нет" || s == "н" || s == "n" || s == "no") return false;

                Print("К сожалению, я не поняла, что вы имели ввиду. Введите: да / нет.", UiRole.Warning);
            }
        }

        public string NormalizeQuestion(string s)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) return "?";
            var first = char.ToUpper(s[0], CultureInfo.CurrentCulture);
            var rest = s.Length > 1 ? s.Substring(1) : "";
            s = first + rest;
            if (!s.EndsWith("?")) s += "?";
            return s;
        }

        public void PrintTree(Node root)
        {
            PrintTreeRec(root, "");
        }

        private void PrintTreeRec(Node node, string indent)
        {
            if (node.IsLeaf)
            {
                Print($"{indent}- {node.ObjectName}", UiRole.Program, withLabel: false);
                return;
            }

            Print($"{indent}? {node.Question}", UiRole.Program, withLabel: false);
            Print($"{indent}  -> Да:", UiRole.Program, withLabel: false);
            if (node.Yes != null) PrintTreeRec(node.Yes, indent + "    ");
            Print($"{indent}  -> Нет:", UiRole.Program, withLabel: false);
            if (node.No != null) PrintTreeRec(node.No, indent + "    ");
        }

        public void PrintLines(IEnumerable<string> lines)
        {
            foreach (var line in lines)
                Print(line, UiRole.Program, withLabel: false);
        }

        private string ReadLineAsRole(UiRole role, bool withLabel)
        {
            var (fg, bg) = ColorsFor(role);
            var oldFg = Console.ForegroundColor;
            var oldBg = Console.BackgroundColor;

            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;

            if (withLabel) Console.Write(LabelFor(role));
            var s = Console.ReadLine() ?? "";

            Console.ForegroundColor = oldFg;
            Console.BackgroundColor = oldBg;
            return s;
        }

        public void PrintTreeFriendly(Node root)
        {
            PrintTreeFriendlyRec(root, "", null);
        }

        private void PrintTreeFriendlyRec(Node node, string indent, string edge)
        {
            if (node.IsLeaf)
            {
                var line = indent;
                if (!string.IsNullOrEmpty(edge)) line += edge + " -> ";
                line += $"{node.ObjectName}(объект)";
                Print(line, UiRole.Program, withLabel: false);
                return;
            }

            var here = indent;
            if (!string.IsNullOrEmpty(edge)) here += edge + " -> ";
            here += $"{node.Question}(вопрос)";
            Print(here, UiRole.Program, withLabel: false);

            if (node.Yes != null) PrintTreeFriendlyRec(node.Yes, indent + "        ", "да");
            if (node.No != null) PrintTreeFriendlyRec(node.No, indent + "        ", "нет");
        }

        public void PrintPathLines(IEnumerable<string> path)
        {
            foreach (var line in path)
                Print(line, UiRole.Program, withLabel: false);
        }


        private static string LabelFor(UiRole role) => role switch
        {
            UiRole.Program => "Программа: ",
            UiRole.Player => "Игрок: ",
            UiRole.Menu => "Меню: ",
            UiRole.Notice => "Программа: ",
            UiRole.Warning => "Программа: ",
            _ => ""
        };

        private static (ConsoleColor fg, ConsoleColor bg) ColorsFor(UiRole role) => role switch
        {
            UiRole.Program => (ConsoleColor.White, ConsoleColor.Black),
            UiRole.Player => (ConsoleColor.Cyan, ConsoleColor.Black),
            UiRole.Menu => (ConsoleColor.Yellow, ConsoleColor.Black),
            UiRole.Notice => (ConsoleColor.Gray, ConsoleColor.Black),
            UiRole.Warning => (ConsoleColor.Yellow, ConsoleColor.Black),
            _ => (ConsoleColor.White, ConsoleColor.Black)
        };
    }
}