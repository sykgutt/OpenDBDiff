using OpenDBDiff.ExternalDiff;
using OpenDBDiff.Settings;
using System;
using System.Windows.Forms;

namespace OpenDBDiff.UI
{
    public partial class ExternalDiffSettingsForm : Form
    {
        public ExternalDiffSettingsForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        public ExternalDiffSettings SavedSettings { get; private set; }

        private void LoadSettings()
        {
            var settings = ExternalDiffSettings.Get();
            txtExecutable.Text = settings.ExecutablePath ?? string.Empty;
            txtFileArguments.Text = string.IsNullOrWhiteSpace(settings.FileArguments)
                ? ExternalDiffSettings.DefaultFileArguments
                : settings.FileArguments;
            txtFolderArguments.Text = string.IsNullOrWhiteSpace(settings.FolderArguments)
                ? ExternalDiffSettings.DefaultFolderArguments
                : settings.FolderArguments;
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*";
                dialog.Title = "Select external comparison tool";
                dialog.CheckFileExists = true;
                if (!string.IsNullOrWhiteSpace(txtExecutable.Text))
                {
                    dialog.FileName = txtExecutable.Text;
                    try
                    {
                        dialog.InitialDirectory = System.IO.Path.GetDirectoryName(txtExecutable.Text);
                    }
                    catch (ArgumentException)
                    {
                    }
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                    txtExecutable.Text = dialog.FileName;
            }
        }

        private void btnWinMerge_Click(object sender, EventArgs e)
        {
            ApplyPreset(ExternalDiffPresets.WinMerge);
        }

        private void btnBeyondCompare_Click(object sender, EventArgs e)
        {
            ApplyPreset(ExternalDiffPresets.BeyondCompare);
        }

        private void ApplyPreset(ExternalDiffPreset preset)
        {
            txtFileArguments.Text = preset.FileArguments;
            txtFolderArguments.Text = preset.FolderArguments;

            var found = ExternalDiffPresets.FindExecutable(preset);
            if (!string.IsNullOrEmpty(found))
            {
                txtExecutable.Text = found;
            }
            else
            {
                MessageBox.Show(this,
                    preset.Name + " was not found in the usual install folders. Browse to the executable; argument templates were filled in.",
                    preset.Name,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            try
            {
                ExternalDiffLauncher.TestExecutable(txtExecutable.Text.Trim());
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "External tool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            var path = (txtExecutable.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show(this, "Select the executable of the external comparison tool.", "External tool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!System.IO.File.Exists(path))
            {
                MessageBox.Show(this, "The selected executable was not found.", "External tool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var settings = ExternalDiffSettings.Get();
            settings.ExecutablePath = path;
            settings.FileArguments = string.IsNullOrWhiteSpace(txtFileArguments.Text)
                ? ExternalDiffSettings.DefaultFileArguments
                : txtFileArguments.Text.Trim();
            settings.FolderArguments = string.IsNullOrWhiteSpace(txtFolderArguments.Text)
                ? ExternalDiffSettings.DefaultFolderArguments
                : txtFolderArguments.Text.Trim();

            ExternalDiffSettings.Save(settings);
            SavedSettings = settings;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
