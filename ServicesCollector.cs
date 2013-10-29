using System;
using System.IO;
using System.Management;

namespace DataCollector
{
    internal class ServicesCollector : ICollector
    {
        public void Collect(IDataCollector collector)
        {
            collector.CreateFileEntry("Services.csv");
            try
            {
                collector.CurrentState = String.Empty;

                StreamWriter writer = new StreamWriter(collector.Stream);

                bool isRunFirst = true;

                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Service"))
                {
                    foreach (ManagementBaseObject obj in searcher.Get())
                    {
                        if (isRunFirst)
                        {
                            foreach (PropertyData property in obj.Properties)
                            {
                                if (!IsDisplay(property))
                                    continue;
                                writer.Write(property.Name);
                                writer.Write(Consts.CsvSeparator);
                            }
                            writer.WriteLine();
                            isRunFirst = false;
                        }
                        foreach (PropertyData property in obj.Properties)
                        {
                            if (!IsDisplay(property))
                                continue;
                            writer.Write(property.Value);
                            writer.Write(Consts.CsvSeparator);
                        }
                        writer.WriteLine();

                        collector.CheckIsStop();
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

        private static bool IsDisplay(PropertyData property)
        {
            switch (property.Name)
            {
                case "AcceptPause":
                case "AcceptStop":
                case "CheckPoint":
                    return false;
            }
            return true;
        }
    }
}