using ProjectManager.SDK.Models;

namespace PmTask;

public class WbsSortHelper : IComparer<TaskDto>
{
    int IComparer<TaskDto>.Compare(TaskDto? x, TaskDto? y)
    {
        if (x == null && y != null)
        {
            return 1;
        }

        if (x != null && y == null)
        {
            return -1;
        }

        if (x == null && y == null)
        {
            return 0;
        }

        var t1 = x!;
        var t2 = y!;

        try
        {
            var t1OutlineNumberArray = t1.Wbs.Trim().Split('.');
            var t2OutlineNumberArray = t2.Wbs.Trim().Split('.');

            for (int i = 0; i < t1OutlineNumberArray.Length; i++)
            {
                if (i >= t2OutlineNumberArray.Length)
                {
                    return 1; // this situation = 2.1 > 2 return 1
                }
                if (int.TryParse(t1OutlineNumberArray[i], out int t1Number) && int.TryParse(t2OutlineNumberArray[i], out int t2Number))
                {
                    if (t1Number == t2Number)
                    {
                        continue; // continue the compare on the next item, e.g 1.1 and 1.2.3 are the same in the first comparsion.
                    }
                    if (t1Number > t2Number)
                    {
                        return 1; // return 1 if t1 > t2 on the same position like '3'.1 > '1'.1
                    }
                    if (t1Number < t2Number)
                    {
                        return -1;
                    }
                }
                else
                {
                    return -1;
                }
            }

            //t1 length > t2 length e.g. 1.1.5 > 1.1, retrun 1. note it passes the compare above so the position can compare will be the same.
            if (t1OutlineNumberArray.Length > t2OutlineNumberArray.Length)
            {
                return 1;
            }
            // e.g. 2.2 < 2.2.15, return -1;
            if (t1OutlineNumberArray.Length < t2OutlineNumberArray.Length)
            {
                return -1;
            }

            // if cannot get answer from above, use index.
            if (t1.Id > t2.Id)
            {
                return 1;
            }
            if (t1.Id < t2.Id)
            {
                return -1;
            }

            return 0;
        }
        catch
        {
            if (t1.Id > t2.Id)
            {
                return 1;
            }
            if (t1.Id < t2.Id)
            {
                return -1;
            }
            return 0;
        }
    }
}