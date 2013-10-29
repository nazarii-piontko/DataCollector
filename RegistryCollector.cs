using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace DataCollector
{
    internal class RegistryCollector : ICollector
    {
        private StreamWriter _writer;
        private readonly Stack<RegistryKey> _keys = new Stack<RegistryKey>();

        public void Collect(IDataCollector collector)
        {
            collector.CreateFileEntry("Registry.csv");
            try
            {
                _writer = new StreamWriter(collector.Stream);
                _writer.WriteLine(String.Concat("Key", Consts.CsvSeparator, "Error", Consts.CsvSeparator));

                CollectRootKey(Registry.ClassesRoot, collector);
                CollectRootKey(Registry.CurrentUser, collector);
                CollectRootKey(Registry.LocalMachine, collector);
                CollectRootKey(Registry.Users, collector);
                CollectRootKey(Registry.CurrentConfig, collector);
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

        private void CollectRootKey(RegistryKey key, IDataCollector collector)
        {
            _keys.Clear();
            _keys.Push(key);

            while (_keys.Count > 0)
            {
                RegistryKey currKey = _keys.Pop();

                if (currKey == null)
                    continue;

                string currKeyName = currKey.ToString();

                collector.CurrentState = currKeyName;
                CollectKey(currKeyName, String.Empty);

                foreach (string keyName in currKey.GetSubKeyNames())
                {
                    try
                    {
                        _keys.Push(currKey.OpenSubKey(keyName));
                    }
                    catch (Exception ex)
                    {
                        if (!IsValidException(ex))
                            throw;
                        CollectKey(currKey + "\\" + keyName, ex.Message);
                    }
                }
                
                currKey.Close();

                collector.CheckIsStop();
            }
        }

        private static bool IsValidException(Exception exception)
        {
            return (exception is SecurityException)
                   || (exception is ArgumentException);
        }

        private void CollectKey(string key, string error)
        {
            _writer.WriteLine(String.Concat(key, Consts.CsvSeparator, error, Consts.CsvSeparator));
        }
    }
}