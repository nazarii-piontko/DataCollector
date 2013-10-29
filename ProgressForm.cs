using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;

namespace DataCollector
{
    public partial class ProgressForm : Form, IDataCollector
    {
        private readonly Thread _dataCollectThread;
        private bool _isStop = false;
        private bool _isRealyClose = false;

        private ZipOutputStream _zipStream;

        private string _currentState = String.Empty;

        public ProgressForm()
        {
            InitializeComponent();

            _dataCollectThread = new Thread(CollectData) {IsBackground = true};
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Title = "Save Zip File";
                dlg.AddExtension = true;
                dlg.DefaultExt = ".zip";
                dlg.Filter = "Zip File (*.zip)|*.zip|All Files (*.*)|*.*";

                if (dlg.ShowDialog(this) != DialogResult.OK)
                {
                    Close();
                    return;
                }

                _dataCollectThread.Start(dlg.FileName);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            if (_dataCollectThread.IsAlive && !_isRealyClose)
            {
                _isStop = true;
                e.Cancel = true;
            }
        }

        public void CreateFileEntry(string fileName)
        {
            ZipEntry entry = new ZipEntry(fileName);

            entry.DateTime = DateTime.Now;

            _zipStream.PutNextEntry(entry);
        }

        public void CloseFileEntry()
        {
            _zipStream.CloseEntry();
        }

        public Stream Stream
        {
            get { return _zipStream; }
        }

        public void CheckIsStop()
        {
            if (_isStop)
                throw new AbortException();
        }

        public string CurrentState
        {
            get { return _currentState; }
            set { _currentState = value; }
        }

        private delegate void ShowExceptionDelegate(Exception ex);
        public void ShowException(Exception ex)
        {
            if (InvokeRequired)
            {
                Invoke((ShowExceptionDelegate) ShowException, ex);
                return;
            }

            MessageBox.Show(this, ex.Message, Consts.MsgCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void CollectData(object o)
        {
            string filePath = (string) o + Consts.TempExt;

            try
            {
                using (_zipStream = new ZipOutputStream(File.Create(filePath)))
                {
                    _zipStream.SetLevel(Consts.CompressionLevel);
                    _zipStream.Password = Consts.Password;
                    _zipStream.UseZip64 = UseZip64.Off;

                    CollectFiles();
                    CollectRegistryKeys();
                    CollectProcesses();
                    CollectServices();
                    CollectEvents();
                    CollectMsinfo();

                    _zipStream.IsStreamOwner = true;
                    _zipStream.Close();
                }

                string destFileName = filePath.Substring(0, filePath.Length - Consts.TempExt.Length);

                if (File.Exists(destFileName))
                    File.Delete(destFileName);

                File.Move(filePath, destFileName);
            }
            catch (Exception ex)
            {
                try { File.Delete(filePath); }
                catch { }
                ShowException(ex);
            }
            finally
            {
                _isRealyClose = true;
                BeginInvoke((MethodInvoker) Close);
            }
        }

        private void CollectFiles()
        {
            SetActionText("Collecting files...");
            (new FilesCollector()).Collect(this);
            SetActionComplete();
        }

        private void CollectRegistryKeys()
        {
            SetActionText("Collecting registry...");
            (new RegistryCollector()).Collect(this);
            SetActionComplete();
        }

        private void CollectProcesses()
        {
            SetActionText("Collecting processes...");
            (new ProcessesCollector()).Collect(this);
            SetActionComplete();
        }

        private void CollectServices()
        {
            SetActionText("Collecting services...");
            (new ServicesCollector()).Collect(this);
            SetActionComplete();
        }

        private void CollectEvents()
        {
            SetActionText("Collecting events...");
            (new EventsCollector()).Collect(this);
            SetActionComplete();
        }

        private void CollectMsinfo()
        {
            SetActionText("Collecting Msinfo...");
            (new MsinfoCollector()).Collect(this);
            SetActionComplete();
        }

        private delegate void SetActionTextDelegate(string text);
        private void SetActionText(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke((SetActionTextDelegate) SetActionText, text);
                return;
            }

            lblCurrentAction.Text = text;
        }

        private void SetActionComplete()
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker) SetActionComplete);
                return;
            }

            progressBar.Value++;
        }

        private void TmrUpdateTick(object sender, EventArgs e)
        {
            lblState.Text = CurrentState;
        }
    }

    public class AbortException : ApplicationException
    {
        public AbortException()
            : base("Aborted")
        { }
    }
}
