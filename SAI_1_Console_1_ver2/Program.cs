using SAI_1_Console_1_ver2.Commands;

namespace SAI_1_Console_1_ver2
{
    class Program
    {
        private const string CMD_PLAY = "давай сыграем";
        private const string CMD_KNOW_ABOUT = "я хочу узнать, что ты знаешь об одном из хобби";
        private const string CMD_SHOW_ALL = "выведи всю базу знаний на экран";
        private const string CMD_KNOWN_CHECK = "я хочу узнать, известно ли тебе о нужном мне хобби";
        private const string CMD_EXIT = "выход";

        static void Main(string[] args)
        {
            var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
            var dbPath = Path.Combine(dataDir, "knowledge.json");
            Directory.CreateDirectory(dataDir);

            var io = new ConsoleIO();
            var storage = new JsonStorage(dbPath);

            io.PrintWelcome();

            while (true)
            {
                io.Print("Чем я теперь могу быть полезна?", UiRole.Program);
                io.PrintMenuQueries();

                var user = io.AskNonEmptyLine("Введите один из запросов:").Trim();
                var norm = Normalize(user);

                if (norm == CMD_EXIT)
                {
                    break;
                }
                else if (norm == CMD_PLAY)
                {
                    new SearchCommand().Run(storage, io);
                    io.Print();
                    continue;
                }
                else if (norm == CMD_SHOW_ALL)
                {
                    var root = storage.LoadOrDefault();
                    io.PrintTreeFriendly(root);
                    io.Print();
                    continue;
                }
                else if (norm == CMD_KNOW_ABOUT)
                {
                    var root = storage.LoadOrDefault();
                    var name = io.AskNonEmptyLine("Без проблем. Назовите хобби, о котором вы хотите меня спросить:").Trim();

                    var path = new List<string>();
                    if (KnowledgeUtils.FindPath(root, name, path))
                    {
                        io.Print($"{name}:", UiRole.Program);
                        io.PrintPathLines(path);
                    }
                    else
                    {
                        io.Print("К сожалению, я ничего не знаю о данном хобби.", UiRole.Program);
                    }
                    io.Print();
                    continue;
                }
                else if (norm == CMD_KNOWN_CHECK)
                {
                    var root = storage.LoadOrDefault();
                    var name = io.AskNonEmptyLine("Напишите, какое именно хобби вас интересует:").Trim();

                    if (KnowledgeUtils.HasObject(root, name))
                    {
                        io.Print("Да, мне известно об этом хобби.", UiRole.Program);
                        if (io.AskYesNo("Хотите узнать, что именно я о нём знаю?"))
                        {
                            var path = new List<string>();
                            KnowledgeUtils.FindPath(root, name, path);
                            io.Print($"{name}:", UiRole.Program);
                            io.PrintPathLines(path);
                        }
                    }
                    else
                    {
                        io.Print("Я ничего не знаю об этом хобби.", UiRole.Program);
                    }
                    io.Print();
                    continue;
                }
                else
                {
                    io.Print("К сожалению, я не поняла, что вы имели ввиду. Введите один из допустимых запросов.", UiRole.Warning);
                    io.Print();
                }
            }

            io.Print("Пока!", UiRole.Program);
        }

        static string Normalize(string s)
        {
            s = (s ?? "").Trim().ToLowerInvariant();
            while (s.EndsWith(".") || s.EndsWith("!") || s.EndsWith("?"))
                s = s[..^1].TrimEnd();
            return s;
        }
    }
}