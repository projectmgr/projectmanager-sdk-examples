using System.Text;
using ProjectManager.SDK.Models;

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
            sb.Append(".");
            return sb.ToString();
        }
    }
    
    /// <summary>
    /// Execute a sync action on a specific data type 
    /// </summary>
    public static async Task<SyncResults> SyncData<T>(
        string category,
        T[] src, 
        T[] dest, 
        AccountMap map, 
        Func<T, string> identityFunc, 
        Func<T, string> primaryKeyFunc,
        Func<T, T, bool> compareFunc,
        Func<T, Task<string>> createFunc, 
        Func<T, T, Task>? updateFunc, 
        Func<T, Task>? deleteFunc)
    {
        var results = new SyncResults();
        
        // Convert our destination list to a dictionary for fast lookup
        var destMap = dest.ToDictionary(d => identityFunc(d));
        var keysToDelete = dest.Select(d => primaryKeyFunc(d)).ToList();
        foreach (var item in src)
        { 
            var identityString = identityFunc(item);
            var primaryKeyString = primaryKeyFunc(item);
            if (destMap.TryGetValue(identityString, out var matchingItem))
            {
                // If the objects are identical, no changes need to be made
                string newPrimaryKey = primaryKeyFunc(matchingItem);
                if (compareFunc(item, matchingItem))
                {
                    keysToDelete.Remove(primaryKeyFunc(matchingItem));
                }
                else
                {
                    // If we are allowed to update, let's do that, but update may not always be an option
                    if (updateFunc != null)
                    {
                        keysToDelete.Remove(primaryKeyFunc(matchingItem));
                        await updateFunc(item, matchingItem);
                        results.Updates++;
                    }
                    else
                    {
                        // If update func is not available, must create new and delete the conflicting old
                        newPrimaryKey = await createFunc(item);
                        results.Creates++;
                    }
                }
                map.Items.Add(new AccountMap.AccountMapItem()
                {
                    Category = category,
                    Identity = identityString,
                    OriginalPrimaryKey = primaryKeyString,
                    NewPrimaryKey = newPrimaryKey,
                });
            }
            else
            {
                var newPrimaryKey = await createFunc(item);
                results.Creates++;
                map.Items.Add(new AccountMap.AccountMapItem()
                {
                    Category = category, 
                    Identity = identityString, 
                    OriginalPrimaryKey = primaryKeyString,
                    NewPrimaryKey = newPrimaryKey,
                });
            }
        }
        
        // If we have the ability to delete, remove items by their primary key on conflicts
        if (deleteFunc != null)
        {
            var destKeyMap = dest.ToDictionary(d => primaryKeyFunc(d));
            foreach (var key in keysToDelete)
            {
                var itemToDelete = destKeyMap[key];
                await deleteFunc(itemToDelete);
                results.Deletes++;
            }
        }

        return results;
    }

    public static void MatchData<T>(
        string category,
        T[] src, 
        T[] dest, 
        AccountMap map, 
        Func<T, string> identityFunc, 
        Func<T, string> primaryKeyFunc)
    {
        var results = new SyncResults();
        
        // Convert our destination list to a dictionary for fast lookup
        var destMap = dest.ToDictionary(d => identityFunc(d));
        var keysToDelete = dest.Select(d => primaryKeyFunc(d)).ToList();
        foreach (var item in src)
        { 
            var identityString = identityFunc(item);
            var primaryKeyString = primaryKeyFunc(item);
            if (destMap.TryGetValue(identityString, out var matchingItem))
            {
                // If the objects are identical, no changes need to be made
                string newPrimaryKey = primaryKeyFunc(matchingItem);
                map.Items.Add(new AccountMap.AccountMapItem()
                {
                    Category = category,
                    Identity = identityString,
                    OriginalPrimaryKey = primaryKeyString,
                    NewPrimaryKey = newPrimaryKey,
                });
            }
        }
    }
}