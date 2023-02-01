using SorterLibrary.Model;

namespace SorterLibrary
{
    public class Sorter
    {
        private const int MAX_FILE_SIZE_TWO_MB = 2 * 1024 * 1024;
        private const char NEW_LINE_SEPARATOR = '\n';
        private const int INPUT_BUFFER_SIZE = 65536;
        private const int OUTPUT_BUFFER_SIZE = 65536;
        private const int FILES_PER_RUN = 10;

        private const string UNSORTED_FILE_EXTENSION = ".unsorted";
        private const string SORTED_FILE_EXTENSION = ".sorted";
        private const string TEMP_FILE_EXTENSION = ".tmp";
        private const string TEMP_FILE_DIRECTORY = "\\TempFiles";

        private string _fileLocation = "";
        private long _maxUnsortedRows;
        private string[] _unsortedRows;
      
        public async Task Run(string source, string output, string fileLocation, 
            CancellationToken cancellation, int maxFileSize = MAX_FILE_SIZE_TWO_MB)
        {
            FileInfo fileInfo = new FileInfo(source);

            _fileLocation = fileLocation + TEMP_FILE_DIRECTORY;

            if (Directory.Exists(_fileLocation))
            {
                Directory.Delete(_fileLocation, true);
            }

            Directory.CreateDirectory(_fileLocation);
                      
            if (fileInfo.Length <= maxFileSize)
            {
                await SortSmallData(source, output);
            }
            else
            {
                var sourceReader = File.OpenRead(source);
                var outputWriter = File.OpenWrite(output);
                await SortLargeData(sourceReader, outputWriter, maxFileSize, cancellation);
            }
        }

        private async Task SortSmallData(string source, string target)
        {
            var inputData = await File.ReadAllLinesAsync(source);            
            var converted = ConverterManager.Convert(inputData);
            var sorted = Sort(converted);
            await File.WriteAllLinesAsync(target, sorted);
        }

        private List<string> Sort(List<LineOfDataModel> source)
        {
            return source.OrderBy(x => x.Number)
                .OrderBy(x => x.Text)
                .Select(x => new string(x.Value))
                .ToList();
        }

        private async Task SortLargeData(Stream source, Stream target,
            int fileMaxSize, CancellationToken cancellationToken)
        {
            var files = await SplitFile(source, fileMaxSize, cancellationToken);
            _unsortedRows = new string[_maxUnsortedRows];
            
            var sortedFiles = await SortFiles(files);

            var done = false;
            var size = FILES_PER_RUN;
            var result = sortedFiles.Count / size;

            while (!done)
            {
                if (result <= 0)
                {
                    done = true;
                }
                result /= size;
            }

            await MergeFiles(sortedFiles, target, cancellationToken);
        }

        private async Task<IReadOnlyCollection<string>> SplitFile(
            Stream sourceStream, int fileMaxSize,
            CancellationToken cancellationToken)
        {
            var fileSize = fileMaxSize;
            var buffer = new byte[fileSize];
            var extraBuffer = new List<byte>();
            var filenames = new List<string>();

            await using (sourceStream)
            {
                var currentFile = 0L;
                while (sourceStream.Position < sourceStream.Length)
                {
                    var totalRows = 0;
                    var runBytesRead = 0;
                    while (runBytesRead < fileSize)
                    {
                        var value = sourceStream.ReadByte();
                        if (value == -1)
                        {
                            break;
                        }

                        var @byte = (byte)value;
                        buffer[runBytesRead] = @byte;
                        runBytesRead++;
                        if (@byte == NEW_LINE_SEPARATOR)
                        {
                            totalRows++;
                        }
                    }

                    var extraByte = buffer[fileSize - 1];

                    while (extraByte != NEW_LINE_SEPARATOR)
                    {
                        var flag = sourceStream.ReadByte();
                        if (flag == -1)
                        {
                            break;
                        }
                        extraByte = (byte)flag;
                        extraBuffer.Add(extraByte);
                    }

                    var filename = $"{++currentFile}.unsorted";
                    await using var unsortedFile = File.Create(Path.Combine(_fileLocation, filename));
                    await unsortedFile.WriteAsync(buffer.AsMemory(0, runBytesRead), cancellationToken);
                    if (extraBuffer.Count > 0)
                    {
                        totalRows++;
                        await unsortedFile.WriteAsync(extraBuffer.ToArray(), 0, extraBuffer.Count, cancellationToken);
                    }

                    if (totalRows > _maxUnsortedRows)
                    {
                        _maxUnsortedRows = totalRows;
                    }

                    filenames.Add(filename);
                    extraBuffer.Clear();
                }

                return filenames;
            }
        }

        private async Task SortFile(Stream unsortedFile, Stream target)
        {
            using var streamReader = new StreamReader(unsortedFile, bufferSize: INPUT_BUFFER_SIZE);
            var counter = 0;
            while (!streamReader.EndOfStream)
            {
                _unsortedRows[counter++] = (await streamReader.ReadLineAsync())!;
            }

            List<LineOfDataModel> list = ConverterManager.Convert(_unsortedRows.Where(x => x is not null).ToArray());
            list = list.OrderBy(x => x.Number).OrderBy(x => x.Text).ToList();
          
            await using var streamWriter = new StreamWriter(target, bufferSize: OUTPUT_BUFFER_SIZE);

            foreach (var item in list)
            {
                await streamWriter.WriteLineAsync(item.Value);
            }

            Array.Clear(_unsortedRows, 0, _unsortedRows.Length);
        }

        private async Task<IReadOnlyList<string>> SortFiles(
            IReadOnlyCollection<string> unsortedFiles)
        {
            var sortedFiles = new List<string>(unsortedFiles.Count);

            foreach (var unsortedFile in unsortedFiles)
            {
                var sortedFilename = unsortedFile.Replace(UNSORTED_FILE_EXTENSION, SORTED_FILE_EXTENSION);
                var unsortedFilePath = Path.Combine(_fileLocation, unsortedFile);
                var sortedFilePath = Path.Combine(_fileLocation, sortedFilename);
                await SortFile(File.OpenRead(unsortedFilePath), File.OpenWrite(sortedFilePath));
                File.Delete(unsortedFilePath);
                sortedFiles.Add(sortedFilename);
            }

            return sortedFiles;
        }

        private async Task MergeFiles(
            IReadOnlyList<string> sortedFiles, Stream target, CancellationToken cancellationToken)
        {
            var done = false;
            while (!done)
            {
                var runSize = FILES_PER_RUN;
                var finalRun = sortedFiles.Count <= runSize;

                if (finalRun)
                {
                    await Merge(sortedFiles, target, cancellationToken);
                    return;
                }
                
                var runs = sortedFiles.Chunk(runSize);
                var chunkCounter = 0;
                foreach (var files in runs)
                {
                    var outputFilename = $"{++chunkCounter}{SORTED_FILE_EXTENSION}{TEMP_FILE_EXTENSION}";
                    if (files.Length == 1)
                    {
                        OverwriteTempFile(files.First(), outputFilename);
                        continue;
                    }

                    var outputStream = File.OpenWrite(GetFullPath(outputFilename));
                    await Merge(files, outputStream, cancellationToken);
                    OverwriteTempFile(outputFilename, outputFilename);

                    void OverwriteTempFile(string from, string to)
                    {
                        File.Move(
                            GetFullPath(from),
                            GetFullPath(to.Replace(TEMP_FILE_EXTENSION, string.Empty)), true);
                    }
                }

                sortedFiles = Directory.GetFiles(_fileLocation, $"*{SORTED_FILE_EXTENSION}")
                    .OrderBy(x =>
                    {
                        var filename = Path.GetFileNameWithoutExtension(x);
                        return int.Parse(filename);
                    })
                    .ToArray();

                if (sortedFiles.Count > 1)
                {
                    continue;
                }

                done = true;
            }
        }

        private async Task Merge(
            IReadOnlyList<string> filesToMerge,
            Stream outputStream,
            CancellationToken cancellationToken)
        {
            var (streamReaders, rows) = await InitializeStreamReaders(filesToMerge);
            var finishedStreamReaders = new List<int>(streamReaders.Length);
            var done = false;
            await using var outputWriter = new StreamWriter(outputStream, bufferSize: OUTPUT_BUFFER_SIZE);

            while (!done)
            {
                rows = rows.OrderBy(x => x.Number).OrderBy(x => x.Text).ToList();

                var valueToWrite = rows[0].Value;
                var streamReaderIndex = rows[0].StreamReader;
                await outputWriter.WriteLineAsync(valueToWrite.AsMemory(), cancellationToken);

                if (streamReaders[streamReaderIndex].EndOfStream)
                {
                    var indexToRemove = rows.FindIndex(x => x.StreamReader == streamReaderIndex);
                    rows.RemoveAt(indexToRemove);
                    finishedStreamReaders.Add(streamReaderIndex);
                    done = finishedStreamReaders.Count == streamReaders.Length;

                    continue;
                }

                var value = await streamReaders[streamReaderIndex].ReadLineAsync(cancellationToken);
                var temp = value!.Split(". ");

                rows[0] = new RowModel
                {
                    Value = value!,
                    StreamReader = streamReaderIndex,
                    Number = int.Parse(temp[0]),
                    Text = temp[1]
                };
            }

            CleanupRun(streamReaders, filesToMerge);
        }

        private async Task<(StreamReader[] StreamReaders, List<RowModel> rows)> InitializeStreamReaders(
            IReadOnlyList<string> sortedFiles)
        {
            var streamReaders = new StreamReader[sortedFiles.Count];
            var rows = new List<RowModel>(sortedFiles.Count);
            for (var i = 0; i < sortedFiles.Count; i++)
            {
                var sortedFilePath = GetFullPath(sortedFiles[i]);
                var sortedFileStream = File.OpenRead(sortedFilePath);
                streamReaders[i] = new StreamReader(sortedFileStream, bufferSize: INPUT_BUFFER_SIZE);
                var value = await streamReaders[i].ReadLineAsync();


                var temp = value!.Split(". ");

                var row = new RowModel
                {
                    Value = value!,
                    StreamReader = i,
                    Number = int.Parse(temp[0]),
                    Text = temp[1]

                };
                rows.Add(row);
            }

            return (streamReaders, rows);
        }

        private string GetFullPath(string filename)
        {
            return Path.Combine(_fileLocation, Path.GetFileName(filename));
        }

        private void CleanupRun(StreamReader[] streamReaders, IReadOnlyList<string> filesToMerge)
        {
            for (var i = 0; i < streamReaders.Length; i++)
            {
                streamReaders[i].Dispose();
                var temporaryFilename = $"{filesToMerge[i]}.removal";
                File.Move(GetFullPath(filesToMerge[i]), GetFullPath(temporaryFilename));
                File.Delete(GetFullPath(temporaryFilename));
            }
        }
    }
}
