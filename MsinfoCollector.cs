using System;
using System.Diagnostics;
using System.IO;
using ICSharpCode.SharpZipLib.Core;

namespace DataCollector
{
    internal class MsinfoCollector : ICollector
    {
        public void Collect(IDataCollector collector)
        {
            collector.CreateFileEntry("Msinfo.nfo");

            try
            {
                collector.CurrentState = String.Empty;

                string fileName = Path.GetTempPath() + "msinfo.nfo";

                Process process = Process.Start("msinfo32.exe", "/nfo " + String.Format("\"{0}\"", fileName));

                if (process == null)
                    throw new ApplicationException("Cannot start Msinfo32");

                while (!process.WaitForExit(100))
                    collector.CheckIsStop();

                if (!File.Exists(fileName))
                    throw new ApplicationException("Msinfo32 is failed or cancelled");

                using (Stream fileStream = File.OpenRead(fileName))
                    StreamUtils.Copy(fileStream, collector.Stream, new byte[4096]);

                try
                {
                    File.Delete(fileName);
                }
                catch
                {
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
    }
}