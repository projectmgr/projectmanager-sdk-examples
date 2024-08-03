using System.Text.Json;
using System.Text.Json.Serialization;
using CSVFile;

namespace PmTask;

public static class OutputHelper
{
    public static void WriteLine(this OutputFormat? format, string message)
    {
        if (format == null)
        {
            Console.WriteLine(message);
        }
    }
    
    public static void WriteItems<T>(IEnumerable<T> list, OutputFormat? format) where T : class, new()
    {
        switch (format)
        {
            case OutputFormat.CSV:
                Console.WriteLine(CSVFile.CSV.Serialize(list, CSVSettings.CSV));
                break;
            case OutputFormat.TSV:
                Console.WriteLine(CSVFile.CSV.Serialize(list, CSVSettings.TSV));
                break;
            case OutputFormat.JSON:
                var jsonSettings = new JsonSerializerOptions()
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = true,
                };
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(list, jsonSettings));
                break;
        }
    }
}