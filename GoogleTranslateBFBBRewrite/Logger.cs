using System;
using System.IO;
using System.Threading;

namespace GoogleTranslateBFBBRewrite;

class Logger
{
    private readonly bool logToFile;
    private readonly string logFilePath;

    public Logger(string filePath, bool logToFile)
    {
        this.logFilePath = filePath;
        this.logToFile = logToFile;
    }

    public void Log(string message)
    {
        Log("INFO", message);
    }

    public void Log(string context, string message)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string threadName = Thread.CurrentThread.Name ?? $"Thread-{Environment.CurrentManagedThreadId}";

        string logMessage = $"[{timestamp}] [{context}] [{threadName}] - {message}";
        
        Console.WriteLine(logMessage);

        if (logToFile)
            File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
    }
}