using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Essy.Tools.InputBox;
using OpenDBDiff.Abstractions.Schema;
using OpenDBDiff.Abstractions.Schema.Misc;
using OpenDBDiff.Abstractions.Schema.Model;
using OpenDBDiff.Abstractions.Ui;
using OpenDBDiff.Extensions;
using OpenDBDiff.ExternalDiff;
using OpenDBDiff.Export;
using OpenDBDiff.Settings;
using ScintillaNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace OpenDBDiff.UI
{
    public partial class MainForm : Form
    {
        private Project ActiveProject;
        private IFront LeftDatabaseSelector;
        private IFront RightDatabaseSelector;
        private IOption Options;
        private List<ISchemaBase> _selectedSchemas = new List<ISchemaBase>();

        private List<IProjectHandler> ProjectHandlers = new List<IProjectHandler>();
        private IProjectHandler ProjectSelectorHandler;

        public MainForm()
        {
            InitializeComponent();

            this.Text = string.Concat(nameof(OpenDBDiff), " v", Assembly.GetExecutingAssembly().GetName().Version.ToString());
        }

        private bool scriptGenerationAllowed;
        private DiffCsvReport csvReport;

        private void SetCsvExportComparison(IDatabase comparison, string sourceName, string targetName)
        {
            csvReport = new DiffCsvReport(comparison, sourceName, targetName);
            btnExportCsv.Enabled = true;
        }

        protected virtual DialogResult ShowCsvSaveDialog(SaveFileDialog dialog)
        {
            return dialog.ShowDialog(this);
        }

        private void btnExportCsv_Click(object sender, EventArgs e)
        {
            if (csvReport == null)
                return;

            try
            {
                using (var dialog = new SaveFileDialog())
                {
                    dialog.Title = "Export comparison to CSV";
                    dialog.Filter = "CSV files (*.csv)|*.csv";
                    dialog.DefaultExt = "csv";
                    dialog.AddExtension = true;
                    dialog.OverwritePrompt = true;
                    dialog.CheckPathExists = true;
                    dialog.FileName = csvReport.SuggestedFileName;
                    if (ShowCsvSaveDialog(dialog) != DialogResult.OK)
                        return;

                    csvReport.Save(dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                HandleException("Exporting comparison to CSV", ex);
            }
        }

        private void SetSynchronizedScript(IDatabase database, ICollection<ISchemaBase> selection)
        {
            txtSyncScript.ReadOnly = false;
            try
            {
                txtSyncScript.ClearAll();
                txtSyncScript.Text = database.ToSqlDiff(selection).ToSQL();
                scriptGenerationAllowed = true;
            }
            catch (OpenDBDiff.SqlServer.Schema.Model.ScriptGenerationException ex)
            {
                txtSyncScript.ClearAll();
                scriptGenerationAllowed = false;
                MessageBox.Show(this, ex.Message, "Script generation blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                txtSyncScript.ReadOnly = true;
                btnCopy.Enabled = scriptGenerationAllowed;
                btnSaveAs.Enabled = scriptGenerationAllowed;
            }
        }

        private void StartComparison()
        {
            btnExportCsv.Enabled = false;
            csvReport = null;
            ProgressForm progress = null;
            string errorLocation = null;
            try
            {
                var sourceName = ProjectSelectorHandler.GetSourceDatabaseName();
                var targetName = ProjectSelectorHandler.GetDestinationDatabaseName();
                if (!String.IsNullOrWhiteSpace(sourceName) && !String.IsNullOrWhiteSpace(targetName))
                {
                    Options = Options ?? this.ProjectSelectorHandler.GetDefaultProjectOptions();
                    var leftGenerator = this.ProjectSelectorHandler.SetSourceGenerator(LeftDatabaseSelector.ConnectionString, Options);
                    var rightGenerator = this.ProjectSelectorHandler.SetDestinationGenerator(RightDatabaseSelector.ConnectionString, Options);
                    IDatabaseComparer databaseComparer = this.ProjectSelectorHandler.GetDatabaseComparer();

                    var leftPair = new KeyValuePair<String, IGenerator>(LeftDatabaseSelector.ToString(), leftGenerator);
                    var rightPair = new KeyValuePair<String, IGenerator>(RightDatabaseSelector.ToString(), rightGenerator);

                    // The progress form will execute the comparer to generate action scripts to migrate the right to the left
                    // Hence, inside the ProgressForm and deeper, right is the origin and left is the destination
                    progress = new ProgressForm(rightPair, leftPair, databaseComparer);
                    progress.ShowDialog(this);
                    if (progress.Error != null)
                    {
                        throw new SchemaException(progress.Error.Message, progress.Error);
                    }

                    txtSyncScript.LexerLanguage = this.ProjectSelectorHandler.GetScriptLanguage();
                    txtSyncScript.ReadOnly = false;
                    errorLocation = "Generating Synchronized Script";
                    _selectedSchemas.Clear();
                    SetSynchronizedScript(progress.Destination, _selectedSchemas);
                    txtSyncScript.ReadOnly = true;
                    txtSyncScript.SetMarginWidth();

                    // Notice again that left is destination, because we generated scripts to migrate the right database to the left.
                    schemaTreeView1.LeftDatabase = progress.Destination;
                    schemaTreeView1.RightDatabase = progress.Origin;

                    schemaTreeView1.OnSelectItem += new SchemaTreeView.SchemaHandler(schemaTreeView1_OnSelectItem);
                    schemaTreeView1_OnSelectItem(schemaTreeView1.SelectedNode);
                    textBox1.Text = progress.Origin.ActionMessage.Message;

                    btnCopy.Enabled = scriptGenerationAllowed;
                    btnSaveAs.Enabled = scriptGenerationAllowed;
                    btnUpdateAll.Enabled = true;
                    btnExternalDiff.Enabled = true;
                    menuCompareAll.Enabled = true;
                    SetCsvExportComparison(progress.Destination, sourceName, targetName);
                }
                else
                    MessageBox.Show(Owner, "Please select a valid connection string", "ERROR", MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                if (errorLocation == null && progress != null)
                {
                    errorLocation = String.Format("{0} (while {1})", progress.ErrorLocation, progress.ErrorMostRecentProgress ?? "initializing");
                }

                throw new SchemaException("Error " + (errorLocation ?? " Comparing Databases"), ex);
            }
        }

        private void schemaTreeView1_OnSelectItem(string nodeFullName)
        {
            try
            {
                txtNewObject.ReadOnly = false;
                txtOldObject.ReadOnly = false;
                txtDiff.ReadOnly = false;

                txtNewObject.ClearAll();
                txtOldObject.ClearAll();
                txtDiff.ClearAll();
                btnCompareTableData.Enabled = false;
                btnUpdate.Enabled = false;

                if (string.IsNullOrEmpty(nodeFullName))
                {
                    menuCompareSelected.Enabled = false;
                    return;
                }

                IDatabase database = (IDatabase)schemaTreeView1.LeftDatabase;
                IDatabase rightDatabase = (IDatabase)schemaTreeView1.RightDatabase;

                ObjectStatus? status;

                status = database.Find(nodeFullName)?.Status;
                if (status.HasValue && status.Value != ObjectStatus.Drop)
                {
                    txtNewObject.Text = database.Find(nodeFullName).ToSql();
                    txtNewObject.SetMarginWidth();
                    if (database.Find(nodeFullName).Status == ObjectStatus.Original)
                    {
                        btnUpdate.Enabled = false;
                    }
                    else
                    {
                        btnUpdate.Enabled = true;
                    }
                }

                btnCompareTableData.Enabled = CanCompareTableData(database?.Find(nodeFullName), rightDatabase?.Find(nodeFullName));

                database = rightDatabase;
                status = database.Find(nodeFullName)?.Status;
                if (status.HasValue && status.Value != ObjectStatus.Create)
                {
                    txtOldObject.Text = database.Find(nodeFullName).ToSql();
                    txtOldObject.SetMarginWidth();
                }
                txtNewObject.ReadOnly = true;
                txtOldObject.ReadOnly = true;

                var diff = (new SideBySideDiffBuilder(new Differ())).BuildDiffModel(txtOldObject.Text, txtNewObject.Text);

                var sb = new StringBuilder();
                DiffPiece newLine, oldLine;
                var markers = new Marker[] { txtDiff.Markers[0], txtDiff.Markers[1], txtDiff.Markers[2], txtDiff.Markers[3] };
                foreach (var marker in markers) marker.Symbol = MarkerSymbol.Background;
                markers[0].SetBackColor(Color.LightGreen); // Imaginary (?)
                markers[1].SetBackColor(Color.LightCyan); // Modified
                markers[2].SetBackColor(Color.LightSalmon); // Deleted
                markers[3].SetBackColor(Color.PeachPuff); // Modified

                var indexes = new List<int>[] { new List<int>(), new List<int>(), new List<int>(), new List<int>() };
                var index = 0;
                for (var i = 0; i < Math.Max(diff.NewText.Lines.Count, diff.OldText.Lines.Count); i++)
                {
                    newLine = i < diff.NewText.Lines.Count ? diff.NewText.Lines[i] : null;
                    oldLine = i < diff.OldText.Lines.Count ? diff.OldText.Lines[i] : null;
                    if (oldLine.Type == ChangeType.Inserted)
                    {
                        sb.AppendLine(" " + oldLine.Text);
                    }
                    else if (oldLine.Type == ChangeType.Deleted)
                    {
                        sb.AppendLine("- " + oldLine.Text);
                        indexes[2].Add(index);
                    }
                    else if (oldLine.Type == ChangeType.Modified)
                    {
                        sb.AppendLine("* " + newLine.Text);
                        indexes[1].Add(index++);
                        sb.AppendLine("* " + oldLine.Text);
                        indexes[3].Add(index);
                    }
                    else if (oldLine.Type == ChangeType.Imaginary)
                    {
                        sb.AppendLine("+ " + newLine.Text);
                        indexes[0].Add(index);
                    }
                    else if (oldLine.Type == ChangeType.Unchanged)
                    {
                        sb.AppendLine("  " + oldLine.Text);
                    }
                    index++;
                }
                txtDiff.Text = sb.ToString();
                txtDiff.SetMarginWidth();
                for (var i = 0; i < 4; i++)
                {
                    foreach (var ind in indexes[i])
                    {
                        txtDiff.Lines[ind].MarkerAdd(i);
                    }
                }
            }
            finally
            {
                txtNewObject.ReadOnly = true;
                txtOldObject.ReadOnly = true;
                txtDiff.ReadOnly = true;
                menuCompareSelected.Enabled = !string.IsNullOrEmpty(txtOldObject.Text) || !string.IsNullOrEmpty(txtNewObject.Text);
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Refresh script when tab is shown
            if (tabControl1.SelectedIndex != 1)
            {
                return;
            }

            var db = schemaTreeView1.LeftDatabase as IDatabase;
            if (db != null)
            {
                this._selectedSchemas = this.schemaTreeView1.GetCheckedSchemas();
                this.txtSyncScript.ReadOnly = false;
                SetSynchronizedScript(db, _selectedSchemas);
                this.txtSyncScript.ReadOnly = true;
                txtSyncScript.SetMarginWidth();
            }
        }

        private static bool CanCompareTableData(ISchemaBase leftItem, ISchemaBase rightItem)
        {
            if (leftItem == null || rightItem == null)
                return false;
            if (leftItem.ObjectType != ObjectType.Table || rightItem.ObjectType != ObjectType.Table)
                return false;
            if (leftItem.HasState(ObjectStatus.Drop) || leftItem.HasState(ObjectStatus.Create))
                return false;
            return true;
        }

        private void btnCompareTableData_Click(object sender, EventArgs e)
        {
            try
            {
                TreeView tree = (TreeView)schemaTreeView1.Controls.Find("treeView1", true)[0];
                ISchemaBase selected = tree.SelectedNode?.Tag as ISchemaBase;
                if (!CanCompareTableData(selected, ((IDatabase)schemaTreeView1.RightDatabase)?.Find(selected?.FullName)))
                {
                    MessageBox.Show(this,
                        "Data compare requires a table that exists in both databases. Dropped or newly created tables cannot be compared.",
                        "Compare data",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                DataCompareForm dataCompare = new DataCompareForm(selected, LeftDatabaseSelector.ConnectionString, RightDatabaseSelector.ConnectionString);
                dataCompare.ShowDialog(this);
            }
            catch (Exception ex)
            {
                HandleException("Comparing table data", ex);
            }
        }

        private void btnExternalDiff_Click(object sender, EventArgs e)
        {
            LaunchExternalDiffSelected();
        }

        private void menuCompareSelected_Click(object sender, EventArgs e)
        {
            LaunchExternalDiffSelected();
        }

        private void menuCompareAll_Click(object sender, EventArgs e)
        {
            LaunchExternalDiffAll();
        }

        private void menuConfigureTool_Click(object sender, EventArgs e)
        {
            ShowExternalDiffSettings();
        }

        private bool EnsureExternalDiffConfigured()
        {
            var settings = ExternalDiffSettings.Get();
            if (settings.IsConfigured)
                return true;

            using (var form = new ExternalDiffSettingsForm())
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                    return false;
            }

            return ExternalDiffSettings.Get().IsConfigured;
        }

        private void ShowExternalDiffSettings()
        {
            using (var form = new ExternalDiffSettingsForm())
            {
                form.ShowDialog(this);
            }
        }

        private void LaunchExternalDiffSelected()
        {
            try
            {
                if (!EnsureExternalDiffConfigured())
                    return;

                if (string.IsNullOrEmpty(schemaTreeView1.SelectedNode) ||
                    (string.IsNullOrEmpty(txtOldObject.Text) && string.IsNullOrEmpty(txtNewObject.Text)))
                {
                    MessageBox.Show(this,
                        "Select an object in the schema tree, or right-click External diff to compare all differences.",
                        "External diff",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                ExternalDiffLauncher.LaunchSelectedObject(
                    ExternalDiffSettings.Get(),
                    txtOldObject.Text,
                    txtNewObject.Text,
                    schemaTreeView1.SelectedNode,
                    RightDatabaseSelector != null ? RightDatabaseSelector.ToString() : "Current",
                    LeftDatabaseSelector != null ? LeftDatabaseSelector.ToString() : "Desired");
            }
            catch (Exception ex)
            {
                HandleException("Opening external diff", ex);
            }
        }

        private void LaunchExternalDiffAll()
        {
            try
            {
                if (!EnsureExternalDiffConfigured())
                    return;

                var left = schemaTreeView1.LeftDatabase as IDatabase;
                var right = schemaTreeView1.RightDatabase as IDatabase;
                if (left == null)
                {
                    MessageBox.Show(this, "Compare two databases before opening an external diff.", "External diff", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var pairs = ExternalDiffObjectCollector.GetDifferences(left, right);
                ExternalDiffLauncher.LaunchAllDifferences(
                    ExternalDiffSettings.Get(),
                    pairs,
                    RightDatabaseSelector != null ? RightDatabaseSelector.ToString() : "Current",
                    LeftDatabaseSelector != null ? LeftDatabaseSelector.ToString() : "Desired");
            }
            catch (Exception ex)
            {
                HandleException("Opening external diff", ex);
            }
        }

        private void btnCompare_Click(object sender, EventArgs e)
        {
            string errorLocation = "Processing Compare";
            try
            {
                Cursor = Cursors.WaitCursor;
                _selectedSchemas = schemaTreeView1.GetCheckedSchemas();
                StartComparison();
                schemaTreeView1.SetCheckedSchemas(_selectedSchemas);
                errorLocation = "Saving Connections";
                Project.SaveLastConfiguration(LeftDatabaseSelector.ConnectionString, RightDatabaseSelector.ConnectionString);
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                HandleException(errorLocation, ex);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void HandleException(string errorLocation, Exception ex)
        {
            var errorDialog = new ErrorForm(ex);
            errorDialog.ShowDialog(this);
        }

        private void UnloadProjectHandler()
        {
            if (ProjectSelectorHandler != null)
            {
                LeftDatabasePanel.Controls.Remove((Control)LeftDatabaseSelector);
                RightDatabasePanel.Controls.Remove((Control)RightDatabaseSelector);
                ProjectSelectorHandler.Unload();
                ProjectSelectorHandler = null;
            }
        }

        private void LoadProjectHandler(IProjectHandler projectHandler)
        {
            UnloadProjectHandler();
            ProjectSelectorHandler = projectHandler;
            LeftDatabaseSelector = ProjectSelectorHandler.CreateSourceSelector();
            RightDatabaseSelector = ProjectSelectorHandler.CreateDestinationSelector();
            LeftDatabasePanel.Controls.Add(LeftDatabaseSelector.Control);
            RightDatabasePanel.Controls.Add(RightDatabaseSelector.Control);
        }

        private void LoadProjectHandler<T>() where T : IProjectHandler, new()
        {
            var handler = new T();
            LoadProjectHandler(handler);
        }

        private void optSybase_CheckedChanged(object sender, EventArgs e)
        {
            /*if (optSybase.Checked)
            {
                this.mySqlConnectFront2 = new OpenDBDiff.Schema.Sybase.Front.AseConnectFront();
                this.mySqlConnectFront1 = new OpenDBDiff.Schema.Sybase.Front.AseConnectFront();
                this.mySqlConnectFront1.Location = new System.Drawing.Point(5, 19);
                this.mySqlConnectFront1.Name = "mySqlConnectFront1";
                this.mySqlConnectFront1.Size = new System.Drawing.Size(410, 214);
                this.mySqlConnectFront1.TabIndex = 10;
                this.mySqlConnectFront2.Location = new System.Drawing.Point(5, 19);
                this.mySqlConnectFront2.Name = "mySqlConnectFront2";
                this.mySqlConnectFront2.Size = new System.Drawing.Size(410, 214);
                this.mySqlConnectFront2.TabIndex = 10;
                this.mySqlConnectFront1.Visible = true;
                this.mySqlConnectFront2.Visible = true;
                this.groupBox3.Controls.Add((System.Windows.Forms.Control)this.mySqlConnectFront2);
                this.groupBox2.Controls.Add((System.Windows.Forms.Control)this.mySqlConnectFront1);
            }
            else
            {
                this.groupBox2.Controls.Remove((System.Windows.Forms.Control)this.mySqlConnectFront1);
                this.groupBox3.Controls.Remove((System.Windows.Forms.Control)this.mySqlConnectFront2);
            }*/
        }

        private void btnSaveAs_Click(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(saveFileDialog1.FileName) && !string.IsNullOrEmpty(Path.GetDirectoryName(saveFileDialog1.FileName)))
                {
                    saveFileDialog1.InitialDirectory = Path.GetDirectoryName(saveFileDialog1.FileName);
                    saveFileDialog1.FileName = Path.GetFileName(saveFileDialog1.FileName);
                }
                saveFileDialog1.ShowDialog(this);
                if (!String.IsNullOrEmpty(saveFileDialog1.FileName))
                {
                    var db = schemaTreeView1.LeftDatabase as IDatabase;
                    if (db != null)
                    {
                        this._selectedSchemas = this.schemaTreeView1.GetCheckedSchemas();
                        var script = db.ToSqlDiff(this._selectedSchemas).ToSQL();
                        using (StreamWriter writer = new StreamWriter(saveFileDialog1.FileName, false))
                        {
                            writer.Write(script);
                            writer.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HandleException("Save Script As", ex);
            }
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            try
            {
                System.Windows.Forms.Clipboard.SetText(txtSyncScript.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error ocurred while trying to copying the text to the clipboard");
                Trace.WriteLine("ERROR: +" + ex.Message);
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            TreeView tree = (TreeView)schemaTreeView1.Controls.Find("treeView1", true)[0];
            try { ((IDatabase)schemaTreeView1.LeftDatabase).ToSqlDiff(schemaTreeView1.GetCheckedSchemas()); }
            catch (Exception ex) { HandleException("Validating updates", ex); return; }
            TreeNode dbArm = tree.Nodes[0];
            var sb = new StringBuilder();

            foreach (TreeNode node in dbArm.Nodes)
            {
                if (node.Nodes.Count != 0)
                {
                    foreach (TreeNode subnode in node.Nodes)
                    {
                        if (subnode.Checked)
                        {
                            //ISchemaBase selected = (ISchemaBase)tree.SelectedNode.Tag;
                            ISchemaBase selected = (ISchemaBase)subnode.Tag;

                            IDatabase database = (IDatabase)schemaTreeView1.LeftDatabase;

                            if (database.Find(selected.FullName) != null)
                            {
                                switch (selected.ObjectType)
                                {
                                    case ObjectType.Table:
                                        {
                                            switch (selected.Status)
                                            {
                                                case ObjectStatus.Create: sb.Append(Updater.createNew(selected, RightDatabaseSelector.ConnectionString)); break;
                                                case ObjectStatus.Alter: sb.Append(Updater.alter(selected, RightDatabaseSelector.ConnectionString)); break;
                                                case ObjectStatus.AlterWhitespace: sb.Append(Updater.alter(selected, RightDatabaseSelector.ConnectionString)); break;
                                                default: sb.AppendLine($"Nothing could be found to do for table '{selected.Name}'"); break;
                                            }
                                        }
                                        break;

                                    case ObjectType.StoredProcedure:
                                        {
                                            switch (selected.Status)
                                            {
                                                case ObjectStatus.Create: sb.Append(Updater.createNew(selected, RightDatabaseSelector.ConnectionString)); break;
                                                case ObjectStatus.Alter: sb.Append(Updater.alter(selected, RightDatabaseSelector.ConnectionString)); break;
                                                case ObjectStatus.AlterWhitespace: sb.Append(Updater.alter(selected, RightDatabaseSelector.ConnectionString)); break;
                                                default: sb.AppendLine($"Nothing could be found to do for stored procedure '{selected.Name}'"); break;
                                            }
                                        }
                                        break;

                                    case ObjectType.Function:
                                        {
                                            switch (selected.Status)
                                            {
                                                case ObjectStatus.Create: sb.Append(Updater.createNew(selected, RightDatabaseSelector.ConnectionString)); break;
                                                case ObjectStatus.Alter: sb.Append(Updater.alter(selected, RightDatabaseSelector.ConnectionString)); break;
                                                case ObjectStatus.AlterWhitespace: sb.Append(Updater.alter(selected, RightDatabaseSelector.ConnectionString)); break;
                                                case ObjectStatus.Alter | ObjectStatus.AlterBody: sb.Append(Updater.alter(selected, RightDatabaseSelector.ConnectionString)); break;
                                                default: sb.AppendLine($"Nothing could be found to do for function '{selected.Name}'"); break;
                                            }
                                        }
                                        break;

                                    case ObjectType.View:
                                        {
                                            switch (selected.Status)
                                            {
                                                case ObjectStatus.Create: sb.Append(Updater.createNew(selected, RightDatabaseSelector.ConnectionString)); break;
                                                case ObjectStatus.Alter: sb.Append(Updater.alter(selected, RightDatabaseSelector.ConnectionString)); break;
                                                case ObjectStatus.AlterWhitespace: sb.Append(Updater.alter(selected, RightDatabaseSelector.ConnectionString)); break;
                                                case ObjectStatus.Alter | ObjectStatus.AlterBody: sb.Append(Updater.alter(selected, RightDatabaseSelector.ConnectionString)); break;
                                                default: sb.AppendLine($"Nothing could be found to do for view '{selected.Name}'"); break;
                                            }
                                        }
                                        break;

                                    default:
                                        {
                                            switch (selected.Status)
                                            {
                                                case ObjectStatus.Create: sb.Append(Updater.addNew(selected, RightDatabaseSelector.ConnectionString)); break;
                                                default: sb.AppendLine($"Nothing could be found to do for '{selected.Name}'"); break;
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            if (sb.Length == 0)
                MessageBox.Show(this, "All successful.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show(this, sb.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            if (Options.Comparison.ReloadComparisonOnUpdate)
            {
                StartComparison();
            }

            btnUpdate.Enabled = false;
        }

        private void btnUpdateAll_Click(object sender, EventArgs e)
        {
            try { ((IDatabase)schemaTreeView1.LeftDatabase).ToSqlDiff(new List<ISchemaBase>()); }
            catch (Exception ex) { HandleException("Validating updates", ex); return; }
            if (MessageBox.Show("Are you sure you want to update all?", "Confirm update", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                TreeView tree = (TreeView)schemaTreeView1.Controls.Find("treeView1", true)[0];
                TreeNode database = tree.Nodes[0];
                var sb = new StringBuilder();
                foreach (TreeNode tn in database.Nodes)
                {
                    foreach (TreeNode inner in tn.Nodes)
                    {
                        if (inner.Tag != null)
                        {
                            ISchemaBase item = (ISchemaBase)inner.Tag;
                            switch (item.Status)
                            {
                                case ObjectStatus.Create: sb.Append(Updater.createNew(item, RightDatabaseSelector.ConnectionString)); break;
                                case ObjectStatus.Alter: sb.Append(Updater.alter(item, RightDatabaseSelector.ConnectionString)); break;
                            }
                        }
                    }
                }

                if (sb.Length == 0)
                    MessageBox.Show(this, "Update successful.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show(this, sb.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                StartComparison();
            }
        }

        private void btnOptions_Click(object sender, EventArgs e)
        {
            Options = Options ?? ProjectSelectorHandler.GetDefaultProjectOptions();
            OptionForm form = new OptionForm(this.ProjectSelectorHandler, Options);
            form.OptionSaved += new OptionControl.OptionEventHandler((option) => Options = option);
            form.ShowDialog(this);
        }

        private void LoadProjectHandlers()
        {
            ProjectHandlers.Clear();
            toolProjectTypes.Items.Clear();

            ProjectHandlers.Add(new SqlServer.Ui.SQLServerProjectHandler());
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadProjectHandlers();
            foreach (var projectHandler in ProjectHandlers)
            {
                toolProjectTypes.Items.Add(projectHandler);
            }

            if (toolProjectTypes.SelectedItem == null && toolProjectTypes.Items.Count > 0)
            {
                toolProjectTypes.SelectedIndex = 0;
            }

            var LastConfiguration = Project.GetLastConfiguration();
            if (LastConfiguration != null)
            {
                if (LeftDatabaseSelector != null)
                    LeftDatabaseSelector.ConnectionString = LastConfiguration.ConnectionStringSource;
                if (RightDatabaseSelector != null)
                    RightDatabaseSelector.ConnectionString = LastConfiguration.ConnectionStringDestination;
            }

            txtNewObject.LexerLanguage = "mssql";
            txtNewObject.ReadOnly = false;
            txtOldObject.LexerLanguage = "mssql";
            txtOldObject.ReadOnly = false;
            txtDiff.LexerLanguage = "mssql";
            txtDiff.ReadOnly = false;
            txtDiff.Margins[0].Width = 20;

            Scintilla[] scintillaControls = new Scintilla[] { txtNewObject, txtOldObject, txtDiff, txtSyncScript };
            foreach (var scintilla in scintillaControls)
            {
                scintilla.InitializeScintillaControls();
            }
            txtSyncScript.Text = "";
            txtSyncScript.SetMarginWidth();
        }

        private void btnSaveProject_Click(object sender, EventArgs e)
        {
            try
            {
                if (ActiveProject == null)
                {
                    ActiveProject = new Project
                    {
                        ConnectionStringSource = ProjectSelectorHandler.GetSourceConnectionString(),
                        ConnectionStringDestination = ProjectSelectorHandler.GetDestinationConnectionString(),
                        ProjectName = String.Format(
                            "[{0}].[{1}] - [{2}].[{3}]",
                            ProjectSelectorHandler.GetSourceServerName(),
                            ProjectSelectorHandler.GetSourceDatabaseName(),
                            ProjectSelectorHandler.GetDestinationServerName(),
                            ProjectSelectorHandler.GetDestinationDatabaseName()
                        ),
                        Options = Options ?? ProjectSelectorHandler.GetDefaultProjectOptions(),
                        Type = Project.ProjectType.SQLServer
                    };

                    var newProjectName = InputBox.ShowInputBox("Enter the project name.", ActiveProject.ProjectName.Trim(), false)?.Trim();

                    if (string.IsNullOrWhiteSpace(newProjectName))
                        return;

                    ActiveProject.ProjectName = newProjectName;
                }
                Project.Upsert(ActiveProject);
            }
            catch (Exception ex)
            {
                HandleException("Saving Project", ex);
            }
        }

        private void btnProject_Click(object sender, EventArgs e)
        {
            try
            {
                var projects = Project.GetAll();
                if (projects.Any())
                {
                    var form = new ListProjectsForm(projects);
                    form.OnSelect += new ListProjectHandler(form_OnSelect);
                    form.OnDelete += new ListProjectHandler(form_OnDelete);
                    form.OnRename += new ListProjectHandler(form_OnRename);
                    form.ShowDialog(this);
                }
                else
                    MessageBox.Show(this, "There are currently no saved projects.", "Projects", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                HandleException("Opening Project", ex);
            }
        }

        private void form_OnRename(Project itemSelected)
        {
            try
            {
                Project.Upsert(itemSelected);
            }
            catch (Exception ex)
            {
                HandleException("Renaming Project", ex);
            }
        }

        private void form_OnDelete(Project itemSelected)
        {
            try
            {
                Project.Delete(itemSelected.Id);
                if ((ActiveProject?.Id).HasValue && ActiveProject.Id == itemSelected.Id)
                {
                    ActiveProject = null;
                    LeftDatabaseSelector.ConnectionString = "";
                    RightDatabaseSelector.ConnectionString = "";
                }
            }
            catch (Exception ex)
            {
                HandleException("Deleting Project", ex);
            }
        }

        private void form_OnSelect(Project itemSelected)
        {
            try
            {
                if (itemSelected != null)
                {
                    ActiveProject = itemSelected;
                    LeftDatabaseSelector.ConnectionString = itemSelected.ConnectionStringSource;
                    RightDatabaseSelector.ConnectionString = itemSelected.ConnectionStringDestination;
                }
            }
            catch (Exception ex)
            {
                HandleException("Selecting Project", ex);
            }
        }

        private void btnNewProject_Click(object sender, EventArgs e)
        {
            LeftDatabaseSelector.ConnectionString = "";
            RightDatabaseSelector.ConnectionString = "";
            ActiveProject = null;
        }

        private void toolProjectTypes_SelectedIndexChanged(object sender, EventArgs e)
        {
            UnloadProjectHandler();
            if (toolProjectTypes.SelectedItem != null)
            {
                var handler = toolProjectTypes.SelectedItem as IProjectHandler;
                LoadProjectHandler(handler);
            }
        }

        private void SwapButton_Click(object sender, EventArgs e)
        {
            var temp = RightDatabaseSelector.Clone() as IFront;
            RightDatabaseSelector.SetSettingsFrom(LeftDatabaseSelector);
            LeftDatabaseSelector.SetSettingsFrom(temp);
        }
    }
}
