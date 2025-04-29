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
}