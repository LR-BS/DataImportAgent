namespace WebnimbusDataImportAgent
{
    public interface IFileWorker
    {
        string DefaultFileFormat { get; set; }
        List<string> GetDirectoryFiles(string FolderPath, string filename);
         List<T> ReadFile<T, M>(string filePath);
    }
}