namespace SAI_1_Console_1_ver2
{
    public class Controller
    {
        private readonly JsonStorage _storage;

        public Controller(string dbPath) => _storage = new JsonStorage(dbPath);

        public Node Load() => _storage.LoadOrDefault();
        public void Save(Node root) => _storage.Save(root);

        public List<string> BuildFriendlyTree(Node root)
        {
            var lines = new List<string>();
            if (root == null) return lines;

            lines.Add(root.IsLeaf ? $"{root.ObjectName}(объект)"
                                  : $"{root.Question}(вопрос)");

            var kids = new List<(Node n, string edge)>();
            if (root.Yes != null) kids.Add((root.Yes, "да"));
            if (root.No != null) kids.Add((root.No, "нет"));

            for (int i = 0; i < kids.Count; i++)
                Walk(kids[i].n, kids[i].edge, prefix: "", isLast: i == kids.Count - 1, lines);

            return lines;

            static void Walk(Node node, string edge, string prefix, bool isLast, List<string> acc)
            {
                string branch = isLast ? "└─ " : "├─ ";
                string nextPrefix = prefix + (isLast ? "   " : "│  ");

                var title = node.IsLeaf ? $"{node.ObjectName}(объект)"
                                        : $"{node.Question}(вопрос)";
                acc.Add(prefix + branch + $"[{edge}] " + title);

                if (node.IsLeaf) return;

                var children = new List<(Node n, string e)>();
                if (node.Yes != null) children.Add((node.Yes, "да"));
                if (node.No != null) children.Add((node.No, "нет"));

                for (int i = 0; i < children.Count; i++)
                    Walk(children[i].n, children[i].e, nextPrefix, i == children.Count - 1, acc);
            }
        }

        public bool Has(string name) => HasObject(Load(), name);

        public static bool HasObject(Node node, string name)
        {
            if (node.IsLeaf) return EqualsName(node.ObjectName, name);
            return (node.Yes != null && HasObject(node.Yes, name))
                || (node.No != null && HasObject(node.No, name));
        }

        public bool TryBuildPath(Node root, string name, out List<string> path)
        {
            path = new List<string>();
            return FindPath(root, name, path);
        }

        public static bool FindPath(Node node, string name, List<string> path)
        {
            if (node.IsLeaf) return EqualsName(node.ObjectName, name);

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

        public static string Normalize(string s)
        {
            if (s == null) return "";
            s = s.Trim().ToLowerInvariant();
            while (s.Length > 0)
            {
                char last = s[s.Length - 1];
                if (last == '.' || last == '!' || last == '?')
                    s = s.Substring(0, s.Length - 1).TrimEnd();
                else break;
            }
            return s;
        }

        public static string NormalizeQuestion(string s)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) return "?";
            char first = char.ToUpper(s[0]);
            string rest = s.Length > 1 ? s.Substring(1) : "";
            s = first + rest;
            if (!s.EndsWith("?")) s += "?";
            return s;
        }

        private static bool EqualsName(string a, string b)
            => string.Equals(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase);
    }
}