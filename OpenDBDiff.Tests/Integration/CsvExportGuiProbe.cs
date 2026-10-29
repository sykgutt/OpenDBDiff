// Standalone WinForms probe with synthetic models: no SQL connections or saved user settings.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using OpenDBDiff.Abstractions.Schema;
using OpenDBDiff.Abstractions.Schema.Model;
using OpenDBDiff.SqlServer.Schema.Model;
using OpenDBDiff.SqlServer.Schema.Options;
using OpenDBDiff.UI;

internal static class CsvExportGuiProbe
{
    [STAThread]
    private static int Main(string[] args)
    {
        var bin = Path.GetFullPath(args[0]);
        AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
        {
            var path = Path.Combine(bin, new AssemblyName(e.Name).Name + ".dll");
            if (!File.Exists(path)) path = Path.ChangeExtension(path, ".exe");
            return File.Exists(path) ? System.Reflection.Assembly.LoadFrom(path) : null;
        };
        try { Run(Path.GetFullPath(args[1])); return 0; }
        catch (Exception ex) { Console.WriteLine(ex); return 1; }
    }

    private sealed class ProbeForm : MainForm
    {
        public string OutputPath;
        public DialogResult NextResult;
        public string SuggestedName;
        public int DialogCount;

        protected override void OnLoad(EventArgs e)
        {
            // Deliberately skip MainForm_Load: this probe must not restore user settings.
        }

        protected override DialogResult ShowCsvSaveDialog(SaveFileDialog dialog)
        {
            DialogCount++;
            SuggestedName = dialog.FileName;
            Check(dialog.AddExtension && dialog.DefaultExt == "csv" && dialog.OverwritePrompt && dialog.CheckPathExists,
                "save dialog checks extension, destination and overwrite");
            Check(dialog.Filter == "CSV files (*.csv)|*.csv", "CSV file filter");
            dialog.FileName = OutputPath;
            return NextResult;
        }
    }

    private static object Field(object target, string name)
    {
        return typeof(MainForm).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    }

    private static object Call(object target, string name, params object[] args)
    {
        return typeof(MainForm).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Console.WriteLine("OK " + message);
    }

    private static void Run(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        using (var form = new ProbeForm())
        {
            form.ShowInTaskbar = false;
            form.Opacity = 0;
            form.Show();
            Application.DoEvents();
            var export = (Button)Field(form, "btnExportCsv");
            var external = (Button)Field(form, "btnExternalDiff");
            var update = (Button)Field(form, "btnUpdateAll");
            var panel = (FlowLayoutPanel)Field(form, "PanelActions");
            Check(!export.Enabled, "export initially disabled");
            Check(panel.Controls.GetChildIndex(export) == panel.Controls.GetChildIndex(external) + 1 &&
                panel.Controls.GetChildIndex(update) == panel.Controls.GetChildIndex(export) + 1,
                "Export CSV is between External diff and Update all");
            Check(panel.AutoScroll && !panel.WrapContents, "actions remain in a vertically scrolling column");

            var comparison = new Database { Name = "Desired", Options = new SqlOption(),
                Info = new DatabaseInfo { Collation = "Latin1_General_100_BIN2" } };
            var unchanged = new Table(comparison) { Owner = "dbo", Name = "Equal", Status = ObjectStatus.Original };
            var changed = new Table(comparison) { Owner = "dbo", Name = "Changed", Status = ObjectStatus.Alter };
            changed.Columns.Add(new Column(changed) { Name = "Nested", Type = "int" });
            var memory = new Table(comparison) { Owner = "inMem", Name = "BlockedMemory",
                IsMemoryOptimized = true, Status = ObjectStatus.Rebuild };
            comparison.Tables.Add(unchanged);
            comparison.Tables.Add(changed);
            comparison.Tables.Add(memory);
            var current = new Database { Name = "Current" };
            var schema = (SchemaTreeView)Field(form, "schemaTreeView1");
            schema.LeftDatabase = comparison;
            schema.RightDatabase = current;
            Call(form, "SetCsvExportComparison", comparison, "Desired", current.Name);
            Check(export.Enabled && !(bool)Field(form, "scriptGenerationAllowed"),
                "comparison enables CSV independently of SQL script availability");

            schema.ShowUnchangedItems = false;
            schema.ShowChangedItems = false;
            schema.SetCheckedSchemas(new List<ISchemaBase> { changed });
            var tree = (TreeView)schema.Controls.Find("treeView1", true)[0];
            tree.CollapseAll();
            current.Name = "OtherSelection";
            form.OutputPath = Path.Combine(outputDirectory, "gui-export.csv");
            File.WriteAllText(form.OutputPath, "Existing file");
            form.NextResult = DialogResult.Cancel;
            export.PerformClick();
            Check(File.ReadAllText(form.OutputPath) == "Existing file", "cancel preserves existing output");
            Check(form.SuggestedName == "DiffCurrent.csv", "filename comes from comparison snapshot");
            form.NextResult = DialogResult.OK;
            export.PerformClick();
            var csv = File.ReadAllText(form.OutputPath);
            Check(csv.Contains("Equal") && csv.Contains("Changed") && csv.Contains("BlockedMemory") && !csv.Contains("Nested"),
                "export ignores filters, checkboxes and collapsed nodes; no nested objects");
            Check(csv.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Length == 4,
                "CSV has one header and three objects");

            panel.PerformLayout();
            panel.ScrollControlIntoView(export);
            using (var image = new Bitmap(form.Width, form.Height))
            {
                form.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
                image.Save(Path.Combine(outputDirectory, "csv-export-panel.png"));
            }

            try { Call(form, "StartComparison"); }
            catch (TargetInvocationException) { }
            Check(!export.Enabled && Field(form, "csvReport") == null, "failed new comparison clears previous export");
            int dialogs = form.DialogCount;
            export.PerformClick();
            Check(form.DialogCount == dialogs, "no save dialog without a completed comparison");
        }
    }
}
