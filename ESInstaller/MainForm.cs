using HDMS.Service;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ESInstaller
{
    public partial class MainForm : Form
    {
        private const int WM_NCCALCSIZE = 0x83;
        private const int WM_NCPAINT = 0x85;
        private int borderWidth = 10;
        private int borderRadius = 10;

        UpdateManager updateManager;
        public MainForm()
        {
            InitializeComponent();
            this.DoubleBuffered = true;

            this.updateManager = new UpdateManager();
            this.updateManager.OnCheckComplete = CheckComplete;
            this.updateManager.OnError = OnError;
            this.updateManager.OnDownloadComplete = OnDownloadComplete;
            this.updateManager.OnDownloadStatusUpdate = ProgressUpdate;
            this.updateManager.OnInstallationComplete = InstallationCompleted;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            btnUpdate.Enabled = false;
            this.Height = 296;
            CheckInstalledVersion();
            //Task.Run(async () => await this.updateManager.GetLatestVersion());
            this.updateManager.GetLatestVersion();
        }

        private void CheckInstalledVersion()
        {
            string version = UpdateManager.CheckInstalledVersion();
            if (version == "Not installed")
            {
                btnUpdate.Text = "Install";
            }
            lbInstalledVersion.Text = version;
        }

        private void ProgressUpdate(int value)
        {
            materialProgressBar1.Value = value;
            lbParcent.Text = value.ToString() + "%";
        }

        private void InstallationCompleted()
        {
            //MessageBox.Show("Update completed");
            this.Height = 352;
            CheckInstalledVersion();
        }

        private void OnDownloadComplete()
        {
            lbStatus.Text = "Status : Installing Version - " + lbLatestVerson.Text;
            this.updateManager.Install();
            lbStatus.Text = "Status : Installed Version - " + lbLatestVerson.Text;
        }

        private void CheckComplete(string version)
        {
            var latestVersion = version.TrimStart('v');
            lbLatestVerson.Text = latestVersion;
            var installedVersion = lbInstalledVersion.Text;
            btnUpdate.Enabled = UpdateManager.HasUpdate(installedVersion, latestVersion);
        }

        public void OnError(string error)
        {
            MessageBox.Show(error);
        }

        private void frmUpdateManager_Load(object sender, EventArgs e)
        {
            btnUpdate.Enabled = false;
            string version = UpdateManager.CheckInstalledVersion();
            lbLatestVerson.Text = version;
            Task.Run(async ()=> await this.updateManager.GetLatestVersion());
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure to update?", "Confirm", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                this.Height = 246;
                lbStatus.Text = "Status : Downloading";
                btnUpdate.Enabled = false;
                this.updateManager.StartDownload();
            }
        }
    }

}
