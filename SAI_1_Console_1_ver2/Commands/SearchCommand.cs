namespace SAI_1_Console_1_ver2.Commands
{
    public class SearchCommand
    {
        public void Run(JsonStorage storage, ConsoleIO io)
        {
            var root = storage.LoadOrDefault();

            io.Print("Давай.", UiRole.Program);
            io.Print();
            io.Print("Правила таковы:", UiRole.Program);
            io.Print("1) Сначала вы загадываете хобби;", UiRole.Program, withLabel: false);
            io.Print("2) После этого я пытаюсь с помощью вопросов его угадать.", UiRole.Program, withLabel: false);
            io.Print();
            io.WaitAnyKey("Нажмите любую клавишу, если уже загадали хобби.");

            io.Print();
            io.Print("Отлично. Начинаем.", UiRole.Program);
            io.Print();

            var trace = new List<string>();
            var node = root;

            while (!node.IsLeaf)
            {
                var q = node.Question ?? "";
                var yes = io.AskYesNo(q);
                trace.Add($"    {q} -> {(yes ? "да" : "нет")}");

                var next = yes ? node.Yes : node.No;
                if (next == null)
                {
                    io.Print();
                    io.Print("Сдаюсь.", UiRole.Program);
                    io.Print();
                    new LearnCommand().Run(storage, io);
                    return;
                }
                node = next;
            }

            var correct = io.AskYesNo($"Это {node.ObjectName}?");
            if (correct)
            {
                io.Print();
                io.Print("Ура. Я выиграла.", UiRole.Program);
                io.Print();
                var explain = io.AskYesNo("Вы хотите, чтобы я объяснила логику ответа?");
                if (explain)
                {
                    io.Print();
                    if (trace.Count > 0) io.PrintLines(trace);
                    io.Print();
                    io.Print($"Следовательно, это {node.ObjectName}.", UiRole.Program);
                }
                return;
            }

            io.Print();
            io.Print("Сдаюсь.", UiRole.Program);
            io.Print();

            new LearnCommand().Run(storage, io);
        }
    }
}