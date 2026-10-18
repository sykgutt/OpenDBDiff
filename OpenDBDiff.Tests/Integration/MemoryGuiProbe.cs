// Standalone integration probe. See Documentacion/Ejecucion.md; not executed by MSTest.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Drawing;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Collections.Generic;
using OpenDBDiff.SqlServer.Schema.Generates;
using OpenDBDiff.SqlServer.Schema.Model;
using OpenDBDiff.SqlServer.Schema.Options;
using OpenDBDiff.Abstractions.Schema;
using OpenDBDiff.Abstractions.Schema.Model;
class MemoryGuiProbe
{
    static string bin;
    [STAThread]
    static int Main(string[] args)
    {
        bin=Path.GetFullPath(args[0]);
        AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{var path=Path.Combine(bin,new AssemblyName(e.Name).Name+".dll");return File.Exists(path)?System.Reflection.Assembly.LoadFrom(path):null;};
        try { Run(args[1]); return 0; } catch(Exception e){Console.WriteLine(e.ToString());return 1;}
    }
    static void Check(bool ok,string msg){if(!ok)throw new Exception(msg);Console.WriteLine("OK "+msg);}
    static Database Read(string db){return new Generate{ConnectionString="Server=localhost;Database="+db+";Integrated Security=True;TrustServerCertificate=True;",Options=new SqlOption()}.Process();}
    static int CatalogCount(string database)
    {
        using(var connection=new SqlConnection("Server=localhost;Database="+database+";Integrated Security=True;TrustServerCertificate=True;"))
        using(var command=new SqlCommand("SELECT COUNT(*) FROM sys.tables",connection))
        { connection.Open(); return (int)command.ExecuteScalar(); }
    }
    static void Run(string imagePath)
    {
        var actual=Read("CuriosityPROD"); var desired=Read("CuriosityPOS");
        Check(actual.Tables.Count==CatalogCount("CuriosityPROD") && desired.Tables.Count==CatalogCount("CuriosityPOS"),"actual catalog counts loaded");
        Console.WriteLine("POS memory="+desired.Tables.Count(t=>t.IsMemoryOptimized)+" PROD memory="+actual.Tables.Count(t=>t.IsMemoryOptimized));
        var compare=Generate.Compare(actual,desired);
        var assembly=System.Reflection.Assembly.LoadFrom(Path.Combine(bin,"OpenDBDiff.exe"));
        var type=assembly.GetType("OpenDBDiff.UI.SchemaTreeView");
        Application.EnableVisualStyles();
        using(var control=(Control)Activator.CreateInstance(type))
        {
            control.Size=new Size(590,780);control.CreateControl();
            type.GetProperty("ShowUnchangedItems").SetValue(control,true,null);
            type.GetProperty("LeftDatabase").SetValue(control,compare,null);
            var tree=(TreeView)control.Controls.Find("treeView1",true)[0];
            var disk=tree.Nodes[0].Nodes["Tables"]; var memory=tree.Nodes[0].Nodes["MemoryOptimizedTables"];
            Check(disk.Nodes.Count+memory.Nodes.Count==compare.Tables.Count,"all comparison tables have one node");
            Check(memory.Nodes.Cast<TreeNode>().All(n=>((Table)n.Tag).IsMemoryOptimized),"memory group contains only memory tables");
            Check(disk.Nodes.Cast<TreeNode>().All(n=>!((Table)n.Tag).IsMemoryOptimized),"disk group excludes memory tables");
            Console.WriteLine(disk.Text);Console.WriteLine(memory.Text);
            var unchanged=compare.Tables.First(t=>t.IsMemoryOptimized && t.Status==ObjectStatus.Original);
            type.GetMethod("SetCheckedSchemas").Invoke(control,new object[]{new List<ISchemaBase>{unchanged}});
            type.GetProperty("ShowUnchangedItems").SetValue(control,false,null);
            type.GetProperty("ShowUnchangedItems").SetValue(control,true,null);
            var selected=(List<ISchemaBase>)type.GetMethod("GetCheckedSchemas").Invoke(control,null);
            Check(selected.Any(s=>s.FullName==unchanged.FullName),"selection survives visibility filters");
            memory=tree.Nodes[0].Nodes["MemoryOptimizedTables"]; memory.Expand();
            tree.Nodes[0].Expand();
            using(var bitmap=new Bitmap(control.Width,control.Height)){control.DrawToBitmap(bitmap,new Rectangle(Point.Empty,control.Size));bitmap.Save(Path.GetFullPath(imagePath));}
            Console.WriteLine("Rendered Schema panel="+Path.GetFullPath(imagePath));
        }
        try { compare.ToSqlDiff(new List<ISchemaBase>()); Console.WriteLine("Full Curiosity script generation allowed (not executed)"); }
        catch(ScriptGenerationException e){Console.WriteLine(e.Message);}
        var diskSelection=compare.Tables.Where(t=>!t.IsMemoryOptimized).Cast<ISchemaBase>().ToList();
        var diskScript=compare.ToSqlDiff(diskSelection).ToSQL();
        Check(diskScript.Length>0,"conventional-table selection generates SQL after excluding blocked memory table (not executed)");
    }
}
