namespace SAI_1_Console_1_ver2
{
    public static class KnowledgeUtils
    {
        public static bool HasObject(Node node, string name)
        {
            if (node.IsLeaf)
                return string.Equals(node.ObjectName ?? "", name, StringComparison.OrdinalIgnoreCase);

            if (node.Yes != null && HasObject(node.Yes, name)) return true;
            if (node.No != null && HasObject(node.No, name)) return true;
            return false;
        }

        public static bool FindPath(Node node, string name, List<string> path)
        {
            if (node.IsLeaf)
                return string.Equals(node.ObjectName ?? "", name, StringComparison.OrdinalIgnoreCase);

            if (node.Yes != null)
            {
                path.Add("        " + (node.Question ?? "") + " -> да");
                if (FindPath(node.Yes, name, path)) return true;
                path.RemoveAt(path.Count - 1);
            }
            if (node.No != null)
            {
                path.Add("        " + (node.Question ?? "") + " -> нет");
                if (FindPath(node.No, name, path)) return true;
                path.RemoveAt(path.Count - 1);
            }
            return false;
        }
    }
}