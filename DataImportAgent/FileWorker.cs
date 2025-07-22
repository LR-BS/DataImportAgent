using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Globalization;
using System.Text;

namespace WebnimbusDataImportAgent;

/// <summary>
/// Class to handle jobs realted to reading webnimbus files.
/// </summary>

public class FileWorker : IFileWorker
{
    public string DefaultFileFormat { get; set; } = "csv";
    /// <summary>
    /// Get list of cvs files with filename matching
    /// </summary>
    /// <param name="FolderPath"> search directory </param>
    /// <param name="filename">search file name</param>
    /// <returns> returns list of cvs files </returns>
    /// <exception cref="ApplicationException">throws applicationException if directory does not exist </exception>
    public List<string> GetDirectoryFiles(string folderPath, string filename)
    {
        if (!Directory.Exists(folderPath)) { throw new ApplicationException($"Directory not found ({folderPath})"); }

        return Directory.EnumerateFiles(folderPath, "*." + DefaultFileFormat, SearchOption.AllDirectories).Where(f => f.Contains(filename)).ToList();
    }

    /// <summary>
    /// Read webnimbus file and convert it to list of records type T using mapper type M
    /// </summary>
    /// <typeparam name="T"> Type of data model </typeparam>
    /// <typeparam name="M"> Type of Mapper</typeparam>
    /// <param name="filePath">webnimbus raw file path </param>
    /// <returns> List of records type T </returns>

    public List<T> ReadFile<T, M>(string filePath)
    {
        List<T> Records = new List<T>();
        List<object> badRecords = new List<object>();
        var config = new CsvConfiguration(CultureInfo.GetCultureInfo("en-US"))
        {
            Mode = CsvMode.NoEscape,
            Delimiter = ";",
            UseNewObjectForNullReferenceMembers = true,
            MissingFieldFound = null,

            Encoding = Encoding.UTF8
        };

        using (var reader = new StreamReader(filePath, Encoding.UTF8))
        using (var csvReader = new CsvReader(reader, config))
        {
            csvReader.Context.TypeConverterOptionsCache.GetOptions<string>().NullValues.Add("NULL");
            csvReader.Context.TypeConverterOptionsCache.GetOptions<DateTime?>().NullValues.AddRange(new[] { "NULL", "0" });
            csvReader.Context.TypeConverterOptionsCache.GetOptions<int?>().NullValues.Add("NULL");
            csvReader.Context.TypeConverterOptionsCache.GetOptions<Boolean>().BooleanFalseValues.Add("N");
            csvReader.Context.TypeConverterOptionsCache.GetOptions<Boolean>().BooleanTrueValues.Add("J");
            csvReader.Context.TypeConverterOptionsCache.GetOptions<Boolean>().BooleanFalseValues.Add("");
            csvReader.Context.TypeConverterOptionsCache.GetOptions<DateTime?>().NullValues.Add("NULL");
            csvReader.Context.TypeConverterOptionsCache.GetOptions<int>().NullValues.Add("NULL");
            csvReader.Context.TypeConverterOptionsCache.GetOptions<Double>().NullValues.Add("NULL");
            csvReader.Context.TypeConverterOptionsCache.GetOptions<Guid?>().NullValues.Add("NULL");

            var options = new TypeConverterOptions { Formats = new[] { "yyyyMMdd", "dd.MM.yyyy", "dd/MM/yyyy" } };
            options.NullValues.Add("null");
            options.NullValues.Add("-");
            csvReader.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
            csvReader.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);

            csvReader.Context.RegisterClassMap(typeof(M));
            while (csvReader.Read())
            {
                // VDMA-602 -- consumptionunit with number=9900 or related devices will be skipped
                if (csvReader.GetField(1) == "9900")
                    continue;
                Records.Add(csvReader.GetRecord<T>()!);
            }
        }

        return Records;
    }
}

public class DoubleConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
    {
        if (double.TryParse(text, out var result))
            return result;

        return 0.0;
    }
}