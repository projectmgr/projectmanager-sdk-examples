namespace PmTask.Clone;

public class AccountMap
{
    public class AccountMapItem()
    {
        public string Category { get; set; }
        public string Identity { get; set; }
        public string OriginalPrimaryKey { get; set; }
        public string NewPrimaryKey { get; set; }
    }

    public List<AccountMapItem> Items { get; set; } = new();

    public string MapKey(string category, string key)
    {
        var match = Items.FirstOrDefault(i => i.Category == category && i.OriginalPrimaryKey == key);
        if (match != null)
        {
            return match.NewPrimaryKey;
        }

        throw new Exception($"No match found for {category} key {key}");
    }
    
    public Guid? MapKeyGuid(string category, Guid? key)
    {
        if (key == null)
        {
            return null;
        }
        
        var guidString = key.ToString();
        var match = Items.FirstOrDefault(i => i.Category == category && i.OriginalPrimaryKey == guidString);
        if (match != null)
        {
            if (Guid.TryParse(match.NewPrimaryKey, out var newGuid))
            {
                return newGuid;
            }
        }

        return null;
    }
}