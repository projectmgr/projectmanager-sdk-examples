using System.Text;

namespace PmTask.Clone;

public class SyncHelper
{
    public class SyncResults
    {
        public int Creates { get; set; }
        public int Updates { get; set; }
        public int Deletes { get; set; }

        public override string ToString()
        {
            if (Creates + Updates + Deletes == 0)
            {
                return "No changes.";
            }
            var sb = new StringBuilder();
            if (Creates > 0)
            {
                sb.Append($"Created {Creates}, ");
            }
            if (Updates > 0)
            {
                sb.Append($"Updated {Updates}, ");
            }
            if (Deletes > 0)
            {
                sb.Append($"Deleted {Deletes}, ");
            }

            sb.Length -= 2;
            return sb.ToString();
        }
    }
    
    /// <summary>
    /// Execute a sync action on a specific data type 
    /// </summary>
    public static async Task<SyncResults> SyncData<T>(T[] src, T[] dest, AccountMap map, 
        Func<T, string> identityFunc, 
        Func<T, string> primaryKeyFunc,
        Func<T, Task<string>> createFunc, 
        Func<T, T, bool> compareFunc,
        Func<T, T, Task> updateFunc, 
        Func<T, Task> deleteFunc)
    {
        var results = new SyncResults();
        
        // Convert our destination list to a dictionary for fast lookup
        var destMap = dest.ToDictionary(d => identityFunc(d));
        var destKeyMap = dest.ToDictionary(d => primaryKeyFunc(d));
        var keysToDelete = dest.Select(d => primaryKeyFunc(d)).ToList();
        foreach (var item in src)
        { 
            var identityString = identityFunc(item);
            var primaryKeyString = primaryKeyFunc(item);
            if (destMap.TryGetValue(identityString, out var matchingItem))
            {
                // Since this key matches and is valid, don't delete it after sync completes
                keysToDelete.Remove(primaryKeyFunc(matchingItem));
                if (!compareFunc(item, matchingItem))
                {
                    await updateFunc(item, matchingItem);
                    results.Updates++;
                }
            }
            else
            {
                var newPrimaryKey = await createFunc(item);
                results.Creates++;
                map.Items.Add(new AccountMap.AccountMapItem()
                {
                    Category = nameof(T), 
                    Identity = identityString, 
                    OriginalPrimaryKey = primaryKeyString,
                    NewPrimaryKey = newPrimaryKey,
                });
            }
        }
        
        // Delete other items that shouldn't continue to exist
        foreach (var key in keysToDelete)
        {
            var itemToDelete = destKeyMap[key];
            await deleteFunc(itemToDelete);
            results.Deletes++;
        }

        return results;
    }
}