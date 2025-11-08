using System.Text.Json;

namespace SAI_1_Console_1_ver2
{
    public class Node
    {
        public string? Question { get; set; }
        public string? ObjectName { get; set; }
        public Node? Yes { get; set; }
        public Node? No { get; set; }

        public bool IsLeaf => Question == null;
    }

    public class JsonStorage
    {
        public string Path { get; }

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true
        };

        public JsonStorage(string path)
        {
            Path = path;
        }

        public Node LoadOrDefault()
        {
            if (!File.Exists(Path))
                return new Node { ObjectName = "Чтение" };

            var json = File.ReadAllText(Path);
            var root = JsonSerializer.Deserialize<Node>(json, Options);
            return root ?? new Node { ObjectName = "Чтение" };
        }

        public void Save(Node root)
        {
            var json = JsonSerializer.Serialize(root, Options);
            File.WriteAllText(Path, json);
        }
    }
}
