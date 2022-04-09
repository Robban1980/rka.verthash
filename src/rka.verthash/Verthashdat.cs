using System.IO.MemoryMappedFiles;
using System.Security.Cryptography;
using System.Text;

namespace RKA.Verthash;

public sealed class Verthashdat : IDisposable
{
    private long _fileSize = 0;
    public long FileSize
    {
        get
        {
            return _fileSize;
        }
    }

    private bool _fileInMemory = false;
    public bool FileInMemory { get { return _fileInMemory; } }

    // Need to keep the file in RAM https://docs.microsoft.com/en-us/dotnet/standard/io/memory-mapped-files
    private MemoryMappedFile _verthashFile;
    public MemoryMappedFile VerthashFile { get { return _verthashFile; } }

    private string _datInMemName = "verthasdat";

    /// <summary>
    /// Big endian
    /// </summary>
    private static string _verthashDatFileHash = "0x48aa21d7afededb63976d48a8ff8ec29d5b02563af4a1110b056cd43e83155a5";
    private static Verthashdat? _instance;
    private string _verthasFileName = "verthash.dat";
    private string _verthashDatFileLocation = "";

    private Verthashdat(string customFileName = "verthash.dat")
    {
        _verthashDatFileLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "vertcoin");
        _verthasFileName = customFileName;
    }

    private Verthashdat(string customPath, string customFileName = "verthash.dat")
    {
        _verthashDatFileLocation = customPath;
        _verthasFileName= customFileName;
    }

    /// <summary>
    /// Get an instance of the verthash data file.
    /// </summary>
    /// <param name="customPath">Provide custom path, leave empty to search the application folder.</param>
    /// <param name="customFileName">Provide alternative name or use default name.</param>
    /// <returns>An instance of <see cref="Verthashdat"/></returns>
    public static Verthashdat GetInstance(string? customPath = null, string customFileName = "verthash.dat")
    {
        if (_instance == null)
        {
            if (string.IsNullOrEmpty(customPath))
            {
                _instance = new Verthashdat(customFileName);
            }
            else
            {
                _instance = new Verthashdat(customPath, customFileName);
            }
        }

        return _instance;
    }

    public bool VerifyDatFile()
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] fileHash = null;

            if (_fileInMemory == false)
            {
                // Load file in to memory
                LoadInRam();

                using (var viewStream = _verthashFile.CreateViewStream())
                {
                    fileHash = sha256Hash.ComputeHash(viewStream);
                }
            }

            using (var viewStream = _verthashFile.CreateViewStream())
            {
                fileHash = sha256Hash.ComputeHash(viewStream);
            }

            var fileStringHash = ByteArrayToString(fileHash);

            if (fileStringHash.Equals(_verthashDatFileHash))
            {
                return true;
            }

            var exception = new ArgumentException("Verthash Datafile's hash is invalid.");
            exception.Data.Add("expectedHash", _verthashDatFileHash);
            exception.Data.Add("fileHash", fileStringHash);
            throw exception;
        }
    }

    /// <summary>
    /// Load verthash dat file in to memory.
    /// </summary>
    public void LoadInRam()
    {
        if (FileInMemory == false)
        {
            string datFile = Path.Combine(_verthashDatFileLocation, _verthasFileName);

            if (File.Exists(datFile) == false)
            {
                var exception = new FileNotFoundException("Verthash datafile not found.");
                exception.Data.Add("pathProvided", datFile);
                throw exception;
            }

            using (FileStream fs = new FileStream(datFile, FileMode.Open, FileAccess.Read))
            {
                _verthashFile = MemoryMappedFile.CreateFromFile(fs, _datInMemName, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, false);
                _fileSize = fs.Length;
            }
            _fileInMemory = true;
        }
    }

    public void Unload()
    {
        if (_verthashFile != null)
        {
            _verthashFile.Dispose();
        }
    }

    public void Dispose()
    {
        Unload();
    }

    /// <summary>
    /// Convert the array to human readable hex
    /// </summary>
    /// <param name="arrInput"></param>
    /// <returns></returns>
    private string ByteArrayToString(byte[] arrInput)
    {
        int i;
        StringBuilder sOutput = new StringBuilder(arrInput.Length);

        if (BitConverter.IsLittleEndian)
        {
            // Reverse
            Array.Reverse(arrInput);
        }

        for (i = 0; i < arrInput.Length; i++)
        {
            sOutput.Append(arrInput[i].ToString("x2"));
        }

        string result = sOutput.ToString();
        return $"0x{result}";
    }
}
