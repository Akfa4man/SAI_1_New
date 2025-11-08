namespace SAI_1_Console_1_ver2.Commands
{
    public class LearnCommand
    {
        public void Run(JsonStorage storage, ConsoleIO io)
        {
            var root = storage.LoadOrDefault();

            if (!io.AskYesNo("Вы загадали хобби?")) return;

            Node? parent = null;
            bool cameFromYes = false;
            var node = root;

            while (!node.IsLeaf)
            {
                var ans = io.AskYesNo(node.Question ?? "");
                parent = node;
                cameFromYes = ans;

                var next = ans ? node.Yes : node.No;
                if (next == null)
                {
                    var obj = io.AskNonEmptyLine("Какое правильное хобби?");
                    var leaf = new Node { ObjectName = obj };
                    if (cameFromYes) parent.Yes = leaf; else parent.No = leaf;
                    storage.Save(root);
                    io.Print("Запомнила новое хобби.", UiRole.Program);
                    return;
                }

                node = next;
            }

            var correct = io.AskYesNo($"Это {node.ObjectName}?");
            if (correct)
            {
                io.Print("Отлично!", UiRole.Program);
                return;
            }

            var correctObj = io.AskNonEmptyLine("Подскажите правильное хобби:");
            var questionRaw = io.AskNonEmptyLine($"Сформулируйте вопрос, отличающий «{correctObj}» от «{node.ObjectName}»:");
            var question = io.NormalizeQuestion(questionRaw);
            var isYesForNew = io.AskYesNo($"Для «{correctObj}» ответ \"да\"");

            var oldName = node.ObjectName;

            node.ObjectName = null;
            node.Question = question;
            if (isYesForNew)
            {
                node.Yes = new Node { ObjectName = correctObj };
                node.No = new Node { ObjectName = oldName };
            }
            else
            {
                node.Yes = new Node { ObjectName = oldName };
                node.No = new Node { ObjectName = correctObj };
            }

            storage.Save(root);
            io.Print("Спасибо! Я запомнила.", UiRole.Program);
        }
    }
}