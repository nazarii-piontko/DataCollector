using System;
using System.IO;

namespace DataCollector
{
    internal interface ICollector
    {
        void Collect(IDataCollector collector);
    }

    internal interface IDataCollector
    {
        Stream Stream { get; }
        string CurrentState { get; set; }

        void CheckIsStop();

        void CreateFileEntry(string name);
        void CloseFileEntry();

        void ShowException(Exception ex);
    }
}