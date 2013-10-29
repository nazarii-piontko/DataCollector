using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace DataCollector
{
    internal class EventsCollector : ICollector
    {
        [DllImport("Advapi32.dll")]
        private extern static bool BackupEventLog(IntPtr hEventLog, string lpBackupFileName);

        [DllImport("Advapi32.dll")]
        private extern static IntPtr OpenEventLog(string lpUNCServerName, string lpSourceName);

        [DllImport("Advapi32.dll")]
        private extern static bool CloseEventLog(IntPtr hEventLog);

        public void Collect(IDataCollector collector)
        {
            try
            {
                byte[] buffer = new byte[4096];

                foreach (EventLog eventLog in EventLog.GetEventLogs())
                {
                    collector.CurrentState = String.Format("{0}/{1}", eventLog.MachineName, eventLog.LogDisplayName);

                    IntPtr handle = OpenEventLog(eventLog.MachineName, eventLog.Log);

                    if (handle == IntPtr.Zero)
                        throw new Win32Exception(Marshal.GetLastWin32Error());

                    try
                    {
                        string log = ZipEntry.CleanName(eventLog.Log);

                        string backupFileName = Path.GetTempPath() + log + ".evt";

                        if (!BackupEventLog(handle, backupFileName))
                            throw new Win32Exception(Marshal.GetLastWin32Error());

                        collector.CreateFileEntry(log + ".evt");
                        try
                        {
                            using (Stream fileStream = File.OpenRead(backupFileName))
                                StreamUtils.Copy(fileStream, collector.Stream, buffer);
                        }
                        finally
                        {
                            collector.CloseFileEntry();
                        }

                        try
                        {
                            File.Delete(backupFileName);
                        }
                        catch
                        {
                        }
                    }
                    catch (Exception ex)
                    {
                        collector.ShowException(ex);
                    }
                    finally
                    {
                        CloseEventLog(handle);
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

            #region Fully managed version - Commented
            //StreamWriter writer = new StreamWriter(collector.Stream);

            //writer.WriteLine(String.Concat("DisplayName", Consts.CsvSeparator
            //                               , "MachineName", Consts.CsvSeparator
            //                               , "Index", Consts.CsvSeparator
            //                               , "Category", Consts.CsvSeparator
            //                               , "EntryType", Consts.CsvSeparator
            //                               , "UserName", Consts.CsvSeparator
            //                               , "Source", Consts.CsvSeparator
            //                               , "TimeGenerated", Consts.CsvSeparator
            //                               , "TimeWritten", Consts.CsvSeparator
            //                               , "Message", Consts.CsvSeparator));

            //foreach (EventLog eventLog in EventLog.GetEventLogs())
            //{
            //    collector.CurrentState = eventLog.LogDisplayName;

            //    foreach (EventLogEntry entry in eventLog.Entries)
            //    {
            //        StringBuilder msgBuilder = new StringBuilder(entry.Message);

            //        msgBuilder.Replace("\"", "\"\"");
            //        msgBuilder.Replace('\n', ' ');

            //        writer.WriteLine(String.Concat(eventLog.LogDisplayName, Consts.CsvSeparator
            //                                       , eventLog.MachineName, Consts.CsvSeparator
            //                                       , entry.Index, Consts.CsvSeparator
            //                                       , entry.Category, Consts.CsvSeparator
            //                                       , entry.EntryType, Consts.CsvSeparator
            //                                       , entry.UserName, Consts.CsvSeparator
            //                                       , entry.Source, Consts.CsvSeparator
            //                                       , entry.TimeGenerated, Consts.CsvSeparator
            //                                       , entry.TimeWritten, Consts.CsvSeparator
            //                                       , "\"" + msgBuilder + "\"", Consts.CsvSeparator));

            //        collector.CheckIsStop();
            //    }
            //}
            #endregion
        }
    }
}