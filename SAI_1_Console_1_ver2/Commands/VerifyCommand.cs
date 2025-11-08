namespace SAI_1_Console_1_ver2.Commands
{
    public class VerifyCommand
    {
        public void Run(JsonStorage storage, ConsoleIO io)
        {
            var root = storage.LoadOrDefault();

            var issues = new List<string>();
            var stats = new Stats();

            Validate(root, issues, stats);

            io.Print("=== Результаты проверки ===", UiRole.Program);
            if (issues.Count == 0)
            {
                io.Print("OK: проблем не обнаружено.", UiRole.Program);
            }
            else
            {
                io.Print("Обнаружены проблемы:", UiRole.Warning);
                for (int i = 0; i < issues.Count; i++)
                    io.Print($"{i + 1}. {issues[i]}", UiRole.Program, withLabel: false);
            }

            io.Print();
            io.Print("=== Статистика базы знаний ===", UiRole.Program);
            io.Print($"Всего узлов:        {stats.TotalNodes}", UiRole.Program, withLabel: false);
            io.Print($"Вопросов:           {stats.QuestionNodes}", UiRole.Program, withLabel: false);
            io.Print($"Объектов (листьев): {stats.LeafNodes}", UiRole.Program, withLabel: false);
            io.Print($"Глубина мин/макс:   {stats.MinDepth} / {stats.MaxDepth}", UiRole.Program, withLabel: false);
            io.Print($"Уникальных объектов: {stats.UniqueObjectNames.Count}", UiRole.Program, withLabel: false);
        }

        private class Stats
        {
            public int TotalNodes;
            public int QuestionNodes;
            public int LeafNodes;
            public int MinDepth = int.MaxValue;
            public int MaxDepth = 0;
            public HashSet<string> UniqueObjectNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private void Validate(Node root, List<string> issues, Stats stats)
        {
            var stack = new Stack<(Node node, int depth)>();
            stack.Push((root, 0));

            while (stack.Count > 0)
            {
                var (n, depth) = stack.Pop();
                stats.TotalNodes++;
                if (depth < stats.MinDepth) stats.MinDepth = depth;
                if (depth > stats.MaxDepth) stats.MaxDepth = depth;

                if (n.IsLeaf)
                {
                    stats.LeafNodes++;
                    if (string.IsNullOrWhiteSpace(n.ObjectName))
                        issues.Add("Лист без названия хобби.");

                    var name = n.ObjectName ?? "";
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        if (!stats.UniqueObjectNames.Add(name))
                            issues.Add($"Дублирующееся имя: \"{name}\".");
                    }

                    if (n.Question != null) issues.Add($"Лист \"{name}\" содержит Question — лишнее.");
                    if (n.Yes != null || n.No != null) issues.Add($"Лист \"{name}\" имеет дочерние узлы — лишнее.");
                }
                else
                {
                    stats.QuestionNodes++;
                    if (string.IsNullOrWhiteSpace(n.Question))
                        issues.Add("У узла-вопроса пустой текст вопроса.");

                    if (n.Yes == null && n.No == null)
                        issues.Add($"Вопрос \"{n.Question}\" не имеет ни одной ветки (Yes/No).");
                    if (n.Yes == null && n.No != null)
                        issues.Add($"Вопрос \"{n.Question}\": отсутствует ветка Yes.");
                    if (n.No == null && n.Yes != null)
                        issues.Add($"Вопрос \"{n.Question}\": отсутствует ветка No.");

                    if (n.Yes != null) stack.Push((n.Yes, depth + 1));
                    if (n.No != null) stack.Push((n.No, depth + 1));
                }
            }

            if (stats.MinDepth == int.MaxValue) stats.MinDepth = 0;
        }
    }
}