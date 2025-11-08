using System;
namespace SAI_1_Console_1_ver2.Commands
{
    public class AnalyzeCommand
    {
        public void Run(JsonStorage storage, ConsoleIO io)
        {
            var root = storage.LoadOrDefault();

            io.Print("=== Анализ базы знаний ===", UiRole.Program);
            io.Print("1) Показать всю базу знаний", UiRole.Program, withLabel: false);
            io.Print("2) Почему такой ответ? (пройти вопросы и вывести трассу)", UiRole.Program, withLabel: false);
            io.Print("3) Есть ли в базе сведения о конкретном хобби?", UiRole.Program, withLabel: false);
            io.Print("4) Показать все сведения (путь) о конкретном хобби", UiRole.Program, withLabel: false);

            var choice = io.AskLine("Выберите пункт (1-4):").Trim();

            if (choice == "1")
            {
                ShowAll(root, io);
            }
            else if (choice == "2")
            {
                ExplainWhy(root, io);
            }
            else if (choice == "3")
            {
                var name = io.AskNonEmptyLine("Введите название хобби:").Trim();
                var found = HasObject(root, name);
                io.Print(found ? "Да, хобби есть в базе." : "Нет, такого хобби нет.", UiRole.Program);
            }
            else if (choice == "4")
            {
                var name = io.AskNonEmptyLine("Введите название хобби:").Trim();
                ShowInfo(root, name, io);
            }
            else
            {
                io.Print("Неизвестный пункт.", UiRole.Warning);
            }
        }

        private void ShowAll(Node root, ConsoleIO io)
        {
            io.Print();
            io.Print("=== Вся база знаний ===", UiRole.Program);
            io.PrintTree(root);
        }
        private void ExplainWhy(Node root, ConsoleIO io)
        {
            var trace = new List<string>();
            var node = root;

            io.Print();
            if (!io.AskYesNo("Вы загадали хобби?"))
            {
                io.Print("Ок.", UiRole.Program);
                return;
            }

            while (!node.IsLeaf)
            {
                var q = node.Question ?? "";
                var yes = io.AskYesNo(q);
                trace.Add((yes ? "Да" : "Нет") + $": {q}");

                var next = yes ? node.Yes : node.No;
                if (next == null)
                {
                    io.Print("Сдаюсь (ветка пустая).", UiRole.Program);
                    io.Print("Пояснение (трасса):", UiRole.Program);
                    io.PrintLines(trace);
                    return;
                }
                node = next;
            }

            io.Print($"Это {node.ObjectName}!", UiRole.Program);
            io.Print("Пояснение (трасса):", UiRole.Program);
            io.PrintLines(trace);
            io.Print($"Вывод: {node.ObjectName}", UiRole.Program);
        }

        private bool HasObject(Node node, string name)
        {
            if (node.IsLeaf)
                return string.Equals(node.ObjectName ?? "", name, StringComparison.OrdinalIgnoreCase);

            bool left = node.Yes != null && HasObject(node.Yes, name);
            if (left) return true;
            return node.No != null && HasObject(node.No, name);
        }

        private void ShowInfo(Node root, string name, ConsoleIO io)
        {
            var path = new List<string>();
            var found = FindPath(root, name, path);
            if (!found)
            {
                io.Print("Сведений нет.", UiRole.Warning);
                return;
            }

            io.Print();
            io.Print($"Хобби: {name}", UiRole.Program);
            io.Print("Путь к хобби:", UiRole.Program);
            io.PrintLines(path);
        }

        private bool FindPath(Node node, string name, List<string> path)
        {
            if (node.IsLeaf)
                return string.Equals(node.ObjectName ?? "", name, StringComparison.OrdinalIgnoreCase);

            if (node.Yes != null)
            {
                path.Add("Да: " + (node.Question ?? ""));
                if (FindPath(node.Yes, name, path)) return true;
                path.RemoveAt(path.Count - 1);
            }

            if (node.No != null)
            {
                path.Add("Нет: " + (node.Question ?? ""));
                if (FindPath(node.No, name, path)) return true;
                path.RemoveAt(path.Count - 1);
            }

            return false;
        }
    }
}