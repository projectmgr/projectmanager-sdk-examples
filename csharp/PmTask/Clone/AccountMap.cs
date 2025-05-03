namespace PmTask.Clone;

public class AccountMap
{
    public class AccountMapItem()
    {
        public string Category { get; set; }
        public string Identity { get; set; }
        public string OriginalPrimaryKey { get; set; }
        public string NewPrimaryKey { get; set; }

        public string GetKey()
        {
            return $"{Category}:{OriginalPrimaryKey}";
        }
    }

    private List<AccountMapItem> Items { get; set; } = new();
    private Dictionary<string, AccountMapItem> Dict { get; set; } = new();

    public void AddItem(AccountMapItem item)
    {
        Items.Add(item);
        Dict[item.GetKey()] = item;
    }

    public Guid? MapKeyGuid(string category, Guid? key)
    {
        if (key == null)
        {
            return null;
        }

        if (Dict.TryGetValue($"{category}:{key}", out var match))
        {
            if (Guid.TryParse(match.NewPrimaryKey, out var newGuid))
            {
                return newGuid;
            }
        }

        return null;
    }

    public List<AccountMapItem> GetItems()
    {
        return Items;
    }
}