using SAI_1_Console_1_ver2;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using UI.Models;

namespace UI
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<MessageItem> Messages { get; } = new();

        string _inputText = "";
        public string InputText { get => _inputText; set { _inputText = value; OnPropertyChanged(); } }

        bool _showQuick = true, _showYN;
        public bool ShowQuickCommands { get => _showQuick; set { _showQuick = value; OnPropertyChanged(); } }
        public bool ShowYesNo { get => _showYN; set { _showYN = value; OnPropertyChanged(); } }

        public ICommand SendCommand { get; }
        public ICommand QuickPlayCommand { get; }
        public ICommand QuickShowAllCommand { get; }
        public ICommand QuickKnowAboutCommand { get; }
        public ICommand QuickKnownCheckCommand { get; }
        public ICommand YesCommand { get; }
        public ICommand NoCommand { get; }

        readonly JsonStorage _storage;
        Node _root, _cur, _parent;
        bool _fromYes;
        string _pendingName = "", _pendingQ = "";
        readonly System.Collections.Generic.List<string> _trace = new();

        enum S { Idle, AskQ, AskGuess, AskExplain, LearnName, LearnQ, LearnAns, AskKnowAbout, AskKnownCheck }
        S _st = S.Idle;

        static readonly string CMD_PLAY = "давай сыграем",
                               CMD_SHOW_ALL = "выведи всю базу знаний на экран",
                               CMD_KNOW_ABOUT = "я хочу узнать, что ты знаешь об одном из хобби",
                               CMD_KNOWN_CHECK = "я хочу узнать, известно ли тебе о нужном мне хобби",
                               CMD_EXIT = "выход";

        public MainWindow()
        {
            InitializeComponent();
            var data = Path.Combine(AppContext.BaseDirectory, "Data");
            Directory.CreateDirectory(data);
            _storage = new JsonStorage(Path.Combine(data, "knowledge.json"));
            _root = _storage.LoadOrDefault();

            SendCommand = new RC(_ => Send());
            QuickPlayCommand = new RC(_ => Push("Давай сыграем"));
            QuickShowAllCommand = new RC(_ => Push("Выведи всю базу знаний на экран"));
            QuickKnowAboutCommand = new RC(_ => Push("Я хочу узнать, что ты знаешь об одном из хобби"));
            QuickKnownCheckCommand = new RC(_ => Push("Я хочу узнать, известно ли тебе о нужном мне хобби"));
            YesCommand = new RC(_ => Push("да"));
            NoCommand = new RC(_ => Push("нет"));

            DataContext = this;
            P(MessageRole.Program, "Добро пожаловать в игру «Угадай хобби».");
            Menu();
        }

        void Send()
        {
            var t = (InputText ?? "").Trim();
            if (t.Length == 0) return;
            P(MessageRole.Player, t);
            InputText = "";
            Handle(t);
        }
        void Push(string t) { P(MessageRole.Player, t); Handle(t); }

        void InputBox_PreviewKeyDown(object s, KeyEventArgs e)
        { if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None) { e.Handled = true; Send(); } }

        void Handle(string raw)
        {
            var x = Norm(raw);
            switch (_st)
            {
                case S.AskQ: YesNoQ(x); return;
                case S.AskGuess: YesNoGuess(x); return;
                case S.AskExplain: YesNoExplain(x); return;
                case S.LearnName: LearnName(raw); return;
                case S.LearnQ: LearnQ(raw); return;
                case S.LearnAns: LearnAns(x); return;
                case S.AskKnowAbout: DoKnowAbout(raw); return;
                case S.AskKnownCheck: DoKnownCheck(raw); return;
            }

            if (_st == S.Idle)
            {
                if (x == CMD_EXIT) { P(MessageRole.Program, "Пока!"); Close(); return; }
                if (x == CMD_PLAY) { Play(); return; }
                if (x == CMD_SHOW_ALL) { ShowAll(); Menu(); return; }
                if (x == CMD_KNOW_ABOUT) { _st = S.AskKnowAbout; Show(false, false); P(MessageRole.Program, "Без проблем. Назовите хобби, о котором вы хотите меня спросить."); return; }
                if (x == CMD_KNOWN_CHECK) { _st = S.AskKnownCheck; Show(false, false); P(MessageRole.Program, "Напишите, какое именно хобби вас интересует."); return; }
                P(MessageRole.Warning, "К сожалению, я не поняла, что вы имели ввиду. Введите один из допустимых запросов."); Menu();
            }
        }

        void Show(bool quick, bool yn) { ShowQuickCommands = quick; ShowYesNo = yn; }
        void Menu()
        {
            Show(true, false);
            P(MessageRole.Program, "Чем я теперь могу быть полезна?");
            P(MessageRole.Menu,
              "Возможные запросы:\n   Давай сыграем.\n   Я хочу узнать, что ты знаешь об одном из хобби.\n   Выведи всю базу знаний на экран.\n   Я хочу узнать, известно ли тебе о нужном мне хобби.\n   Выход.");
        }

        void Play()
        {
            _root = _storage.LoadOrDefault();
            _cur = _root; _parent = null; _fromYes = false; _trace.Clear();
            Show(false, false);
            P(MessageRole.Program, "Давай.");
            P(MessageRole.Program, "Правила таковы:\n1) Сначала вы загадываете хобби;\n2) После этого я пытаюсь с помощью вопросов его угадать.");
            P(MessageRole.Notice, "Начинаем.");
            AskNext();
        }
        void AskNext()
        {
            if (!_cur.IsLeaf) { _st = S.AskQ; P(MessageRole.Program, FixQ(_cur.Question)); Show(false, true); }
            else { _st = S.AskGuess; P(MessageRole.Program, $"Это {_cur.ObjectName}?"); Show(false, true); }
        }
        void YesNoQ(string a)
        {
            var y = YN(a); if (y == null) { WarnYN(); return; }
            Show(false, false);
            _trace.Add("    " + (_cur.Question ?? "") + " -> " + (y.Value ? "да" : "нет"));
            _parent = _cur; _fromYes = y.Value;
            var next = y.Value ? _cur.Yes : _cur.No;
            if (next == null) { P(MessageRole.Program, "Сдаюсь."); _st = S.LearnName; P(MessageRole.Program, "Какое хобби вы загадали?"); return; }
            _cur = next; AskNext();
        }
        void YesNoGuess(string a)
        {
            var y = YN(a); if (y == null) { WarnYN(); return; }
            Show(false, false);
            if (y.Value) { P(MessageRole.Program, "Ура. Я выиграла."); _st = S.AskExplain; P(MessageRole.Program, "Вы хотите, чтобы я объяснила логику ответа?"); Show(false, true); }
            else { P(MessageRole.Program, "Сдаюсь."); _st = S.LearnName; P(MessageRole.Program, "Какое хобби вы загадали?"); }
        }
        void YesNoExplain(string a)
        {
            var y = YN(a); if (y == null) { WarnYN(); return; }
            Show(false, false);
            if (y.Value) { foreach (var t in _trace) P(MessageRole.Program, t); P(MessageRole.Program, $"Следовательно, это {_cur.ObjectName}."); }
            _st = S.Idle; Menu();
        }
        void WarnYN() { P(MessageRole.Warning, "Пожалуйста, ответьте «да» или «нет»."); Show(false, true); }

        void LearnName(string raw)
        {
            _pendingName = raw.Trim();
            if (_pendingName.Length == 0) { P(MessageRole.Warning, "Похоже, что вы ввели пустую строку. Повторите ввод."); return; }
            _st = S.LearnQ; P(MessageRole.Program, $"Сформулируйте вопрос, который поможет распознать хобби {_pendingName}.");
        }
        void LearnQ(string raw)
        {
            _pendingQ = FixQ(raw); _st = S.LearnAns;
            P(MessageRole.Program, "Подскажите правильный ответ на него: да или нет."); Show(false, true);
        }
        void LearnAns(string a)
        {
            var y = YN(a); if (y == null) { WarnYN(); return; }
            Show(false, false);
            if (_cur.IsLeaf)
            {
                var old = _cur.ObjectName; _cur.ObjectName = null; _cur.Question = _pendingQ;
                if (y.Value) { _cur.Yes = new Node { ObjectName = _pendingName }; _cur.No = new Node { ObjectName = old }; }
                else { _cur.Yes = new Node { ObjectName = old }; _cur.No = new Node { ObjectName = _pendingName }; }
            }
            else
            {
                var leaf = new Node { ObjectName = _pendingName };
                if (_fromYes) _parent.Yes = leaf; else _parent.No = leaf;
            }
            _storage.Save(_root);
            P(MessageRole.Program, $"Отлично. Теперь я знаю о хобби {_pendingName}. Спасибо за информацию.");
            _pendingName = _pendingQ = ""; _trace.Clear(); _st = S.Idle; Menu();
        }

        void DoKnowAbout(string raw)
        {
            var name = raw.Trim(); var path = new System.Collections.Generic.List<string>();
            _root = _storage.LoadOrDefault();
            if (KnowledgeUtils.FindPath(_root, name, path))
            {
                var sb = new StringBuilder().AppendLine($"{name}:");
                foreach (var l in path) sb.AppendLine(l);
                P(MessageRole.Program, sb.ToString().TrimEnd(), true);
            }
            else P(MessageRole.Program, "К сожалению, я ничего не знаю о данном хобби.");
            _st = S.Idle; Menu();
        }
        void DoKnownCheck(string raw)
        {
            var name = raw.Trim(); _root = _storage.LoadOrDefault();
            if (KnowledgeUtils.HasObject(_root, name))
            { P(MessageRole.Program, "Да, мне известно об этом хобби."); _st = S.AskExplain; P(MessageRole.Program, "Хотите узнать, что именно я о нём знаю?"); Show(false, true); }
            else { P(MessageRole.Program, "Я ничего не знаю об этом хобби."); _st = S.Idle; Menu(); }
        }
        void ShowAll() { _root = _storage.LoadOrDefault(); P(MessageRole.Program, Tree(_root), true); }

        void P(MessageRole role, string text, bool block = false)
        {
            Messages.Add(new MessageItem { Text = text, Role = role });
            ChatScroll?.ScrollToEnd();
        }

        static string FixQ(string s)
        {
            s = (s ?? "").Trim(); if (s.Length == 0) return "?";
            var first = char.ToUpper(s[0]); var rest = s.Length > 1 ? s.Substring(1) : "";
            if (rest.Length > 0) s = first + rest; else s = first.ToString();
            if (!s.EndsWith("?")) s += "?"; return s;
        }
        static string Norm(string s)
        {
            if (s == null) return ""; s = s.Trim().ToLowerInvariant();
            while (s.Length > 0)
            {
                var c = s[^1]; if (c == '.' || c == '!' || c == '?') s = s[..^1].TrimEnd(); else break;
            }
            return s;
        }
        static bool? YN(string n) => n == "да" || n == "y" || n == "yes" ? true : n == "нет" || n == "n" || n == "no" ? false : (bool?)null;

        static string Tree(Node r)
        {
            var sb = new StringBuilder(); void Walk(Node n, string ind, string edge)
            {
                if (n.IsLeaf) { sb.Append(ind); if (edge != null) sb.Append(edge).Append(" -> "); sb.Append(n.ObjectName).AppendLine("(объект)"); return; }
                sb.Append(ind); if (edge != null) sb.Append(edge).Append(" -> "); sb.Append(n.Question).AppendLine("(вопрос)");
                if (n.Yes != null) Walk(n.Yes, ind + "        ", "да");
                if (n.No != null) Walk(n.No, ind + "        ", "нет");
            }
            Walk(r, "", null); return sb.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

    public sealed class RC : ICommand
    {
        readonly Action<object> _run; readonly Func<object, bool> _can;
        public RC(Action<object> run, Func<object, bool> can = null) { _run = run; _can = can; }
        public bool CanExecute(object p) => _can == null || _can(p);
        public void Execute(object p) => _run(p);
        public event EventHandler CanExecuteChanged { add { } remove { } }
    }
}