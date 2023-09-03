using HDMS.Service;
using System;
using System.Windows.Forms;

namespace ESInstaller
{
    public partial class MainForm : Form
    {
        UpdateManager updateManager;
        public MainForm()
        {
            InitializeComponent();

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
            string version = UpdateManager.CheckInstalledVersion();
            lbInstalledVersion.Text = version;
            this.updateManager.GetLatestVersion();
        }

        private void ProgressUpdate(int value)
        {
            materialProgressBar1.Value = value;
            lbParcent.Text = value.ToString() + "%";
        }

        private void InstallationCompleted()
        {
            MessageBox.Show("Update completed");
        }

        private void OnDownloadComplete()
        {
            lbStatus.Text = "Status : Installing SIBL";
            this.updateManager.Install();
            lbStatus.Text = "Status : SIBL Updated";
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
            this.updateManager.GetLatestVersion();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure to update?", "Confirm", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                lbStatus.Text = "Status : Downloading";
                btnUpdate.Enabled = false;
                this.updateManager.StartDownload();
            }
        }
    }
}
