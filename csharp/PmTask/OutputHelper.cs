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
        var settings = new CSVSettings()
        {
            NestedArrayBehavior = ArrayOptions.RecursiveSerialization,
            NestedObjectBehavior = ObjectOptions.RecursiveSerialization,
        };
        switch (format)
        {
            case OutputFormat.CSV:
                Console.WriteLine(CSVFile.CSV.Serialize(list, settings));
                break;
            case OutputFormat.TSV:
                settings.FieldDelimiter = '\t';
                Console.WriteLine(CSVFile.CSV.Serialize(list, settings));
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