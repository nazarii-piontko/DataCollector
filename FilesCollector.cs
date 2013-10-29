using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DataCollector
{
    internal class FilesCollector : ICollector
    {
        private StreamWriter _writer;
        private readonly Stack<string> _directories = new Stack<string>();

        public void Collect(IDataCollector collector)
        {
            collector.CreateFileEntry("Files.csv");
            try
            {
                _writer = new StreamWriter(collector.Stream);
                _writer.WriteLine(String.Concat("File", Consts.CsvSeparator
                                                , "Length", Consts.CsvSeparator
                                                , "Attributes", Consts.CsvSeparator
                                                , "CreationTime", Consts.CsvSeparator
                                                , "LastAccessTime", Consts.CsvSeparator
                                                , "LastWriteTime", Consts.CsvSeparator

                                                , "FileVersion", Consts.CsvSeparator
                                                , "CompanyName", Consts.CsvSeparator
                                                , "LegalCopyright", Consts.CsvSeparator
                                                , "LegalTrademarks", Consts.CsvSeparator
                                                , "Language", Consts.CsvSeparator
                                                , "FileDescription", Consts.CsvSeparator
                                                , "Comments", Consts.CsvSeparator
                                                , "Error", Consts.CsvSeparator));

                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    try
                    {
                        if (drive.DriveType != DriveType.Fixed || !drive.IsReady)
                            continue;

                        CollectDrive(drive.RootDirectory.FullName, collector);
                    }
                    catch (AbortException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        collector.ShowException(ex);
                    }
                }
            }
            catch (AbortException)
            {
                throw;
            }
            catch (Exception ex)
            {
                collector.ShowException(ex);
            }
            finally
            {
                collector.CloseFileEntry();
            }
        }

        private void CollectDrive(string drive, IDataCollector collector)
        {
            _directories.Clear();
            _directories.Push(drive);

            while (_directories.Count > 0)
            {
                string currDir = _directories.Pop();

                try
                {
                    foreach (string file in Directory.GetFiles(currDir))
                    {
                        try
                        {
                            CollectFile(file);
                        }
                        catch (Exception ex)
                        {
                            if (!IsValidException(ex))
                                throw;

                            WriteError(currDir, ex);
                        }
                    }

                    foreach (string dir in Directory.GetDirectories(currDir))
                        _directories.Push(dir);
                }
                catch (Exception ex)
                {
                    if (!IsValidException(ex))
                        throw;

                    WriteError(currDir, ex);
                }

                collector.CurrentState = currDir;
                collector.CheckIsStop();
            }
        }

        private void WriteError(string file, Exception ex)
        {
            _writer.WriteLine(String.Concat(file, Consts.CsvSeparator
                                            , String.Empty, Consts.CsvSeparator
                                            , String.Empty, Consts.CsvSeparator
                                            , String.Empty, Consts.CsvSeparator
                                            , String.Empty, Consts.CsvSeparator
                                            , String.Empty, Consts.CsvSeparator
                                            , String.Empty, Consts.CsvSeparator
                                            , String.Empty, Consts.CsvSeparator
                                            , String.Empty, Consts.CsvSeparator
                                            , String.Empty, Consts.CsvSeparator
                                            , String.Empty, Consts.CsvSeparator
                                            , String.Empty, Consts.CsvSeparator
                                            , String.Empty, Consts.CsvSeparator
                                            , "\"", ex.Message, "\"", Consts.CsvSeparator));
        }

        private static bool IsValidException(Exception exception)
        {
            //return (exception is UnauthorizedAccessException)
            //       || (exception is PathTooLongException)
            //       || (exception is DirectoryNotFoundException);
            return true;
        }

        private void CollectFile(string file)
        {
            FileInfo info = new FileInfo(file);
            FileVersionInfo versionInfo = IsGetVersionInfo(file) ? FileVersionInfo.GetVersionInfo(file) : null;

            _writer.WriteLine(String.Concat(file, Consts.CsvSeparator
                                            , info.Length, Consts.CsvSeparator
                                            , info.Attributes, Consts.CsvSeparator
                                            , info.CreationTime, Consts.CsvSeparator
                                            , info.LastAccessTime, Consts.CsvSeparator
                                            , info.LastWriteTime, Consts.CsvSeparator

                                            , versionInfo == null ? String.Empty : versionInfo.FileVersion, Consts.CsvSeparator
                                            , versionInfo == null ? String.Empty : EscapeString(versionInfo.CompanyName), Consts.CsvSeparator
                                            , versionInfo == null ? String.Empty : EscapeString(versionInfo.LegalCopyright), Consts.CsvSeparator
                                            , versionInfo == null ? String.Empty : EscapeString(versionInfo.LegalTrademarks), Consts.CsvSeparator
                                            , versionInfo == null ? String.Empty : versionInfo.Language, Consts.CsvSeparator
                                            , versionInfo == null ? String.Empty : EscapeString(versionInfo.FileDescription), Consts.CsvSeparator
                                            , versionInfo == null ? String.Empty : EscapeString(versionInfo.Comments), Consts.CsvSeparator

                                            , String.Empty, Consts.CsvSeparator));
        }

        private static readonly StringBuilder _builder = new StringBuilder(4096);

        private static string EscapeString(string text)
        {
            if (String.IsNullOrEmpty(text))
                return text;

            _builder.Length = 0;
            _builder.Append('"');
            _builder.Append(text);
            _builder.Append('"');

            _builder
                .Replace("\"", "\"\"", 1, _builder.Length - 2)
                .Replace(Environment.NewLine, " ", 1, _builder.Length - 2);

            return _builder.ToString();
        }

        /// <summary>
        /// Used for perfomance reasons. FileVersionInfo.GetVersionInfo works to slow due to OS specific
        /// </summary>
        private static bool IsGetVersionInfo(string file)
        {
            string extension = Path.GetExtension(file);

            if (extension == null)
                return false;

            switch (extension.ToUpper())
            {
                case ".EXE":
                case ".DLL":
                case ".OCX":

                case ".DOC":
                case ".DOCX":

                case ".XLS":
                case ".XLSX":

                case "PPT":
                case "PPTX":

                case "XSF":
                    return true;
            }

            return false;
        }
    }
}