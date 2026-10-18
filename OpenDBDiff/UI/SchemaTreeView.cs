using OpenDBDiff.SqlServer.Schema.Model;
using OpenDBDiff.Abstractions.Schema;
using OpenDBDiff.Abstractions.Schema.Attributes;
using OpenDBDiff.Abstractions.Schema.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace OpenDBDiff.UI
{
    public partial class SchemaTreeView : UserControl
    {
        private ISchemaBase databaseSource;

        public delegate void SchemaHandler(string ObjectFullName);

        public event SchemaHandler OnSelectItem;

        private bool busy = false;
        private readonly Dictionary<string, ISchemaBase> checkedItems = new Dictionary<string, ISchemaBase>();

        private static string ItemKey(ISchemaBase item) { return item.ObjectType + ":" + item.FullName; }

        private void RememberCheckedNodes(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                var item = node.Tag as ISchemaBase;
                if (item != null)
                {
                    if (node.Checked) checkedItems[ItemKey(item)] = item;
                    else checkedItems.Remove(ItemKey(item));
                }
                RememberCheckedNodes(node.Nodes);
            }
        }

        public SchemaTreeView()
        {
            InitializeComponent();
            treeView1.ShowNodeToolTips = true;
        }

        public ISchemaBase RightDatabase { get; set; }

        public ISchemaBase LeftDatabase
        {
            get { return databaseSource; }
            set
            {
                if (!ReferenceEquals(databaseSource, value))
                {
                    checkedItems.Clear();
                    treeView1.Nodes.Clear();
                }
                databaseSource = value;
                if (value != null)
                {
                    RebuildSchemaTree();
                }
            }
        }

        public List<ISchemaBase> GetCheckedSchemas()
        {
            if (treeView1.CheckBoxes)
            {
                RememberCheckedNodes(treeView1.Nodes);
            }
            return checkedItems.Values.ToList();
        }

        public void SetCheckedSchemas(List<ISchemaBase> schemas)
        {
            checkedItems.Clear();
            foreach (var item in schemas) checkedItems[ItemKey(item)] = item;
            SetCheckedNodesFromList(schemas, treeView1.Nodes);
        }

        private void SetCheckedNodesFromList(List<ISchemaBase> schemas, TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag != null)
                {
                    var item = (ISchemaBase)node.Tag;
                    node.Checked = schemas.Any(sch => sch.ObjectType == item.ObjectType && sch.FullName == item.FullName);
                }
                SetCheckedNodesFromList(schemas, node.Nodes);
            }
        }

        private void ReadProperties(Type item, TreeNodeCollection nodes, ISchemaBase schema)
        {
            PropertyInfo[] pi = item.GetProperties();
            nodes.Clear();
            foreach (PropertyInfo p in pi)
            {
                object[] attrs = p.GetCustomAttributes(typeof(SchemaNodeAttribute), true);
                if (attrs.Length > 0)
                {
                    SchemaNodeAttribute show = (SchemaNodeAttribute)attrs[0];
                    TreeNode node = nodes.Add(p.Name, show.Name);
                    node.ImageKey = "Folder";
                    if (schema is Database && p.Name == "Tables")
                    {
                        var tables = ((Database)schema).Tables;
                        ReadItems(node, tables.Where(t => !t.IsMemoryOptimized).Cast<ISchemaBase>().ToList(), show);
                        var memoryNode = nodes.Add("MemoryOptimizedTables", "Memory-Optimized Tables");
                        memoryNode.ImageKey = "Folder";
                        ReadItems(memoryNode, tables.Where(t => t.IsMemoryOptimized).Cast<ISchemaBase>().ToList(), show);
                    }
                    else ReadPropertyDetail(node, p, schema, show);
                }
            }
        }

        private void ReadPropertyDetail(TreeNode node, PropertyInfo p, ISchemaBase schema, SchemaNodeAttribute attr)
        {
            ReadItems(node, ((IList)p.GetValue(schema, null)).Cast<ISchemaBase>().ToList(), attr);
        }

        private void ReadItems(TreeNode node, IList<ISchemaBase> items, SchemaNodeAttribute attr)
        {
            Color NodeColor = Color.Black;
            node.Text = node.Text + " (" + items.Count(CanNodeAdd) + " / " + items.Count + ")";
            node.Nodes.Clear();
            foreach (ISchemaBase item in items)
            {
                if (CanNodeAdd(item))
                {
                    TreeNode subnode = node.Nodes.Add(ItemKey(item), (attr.IsFullName ? item.FullName : item.Name));
                    if (item.Status == ObjectStatus.Drop)
                    {
                        subnode.ForeColor = Color.Red;
                        NodeColor = (NodeColor == Color.Black || NodeColor == Color.Red ? Color.Red : Color.Plum);
                    }
                    if (item.Status == ObjectStatus.Create)
                    {
                        subnode.ForeColor = Color.Green;
                        NodeColor = (NodeColor == Color.Black || NodeColor == Color.Green ? Color.Green : Color.Plum);
                    }
                    if ((item.HasState(ObjectStatus.Alter)) || (item.HasState(ObjectStatus.Disabled)))
                    {
                        subnode.ForeColor = Color.Blue;
                        NodeColor = (NodeColor == Color.Black || NodeColor == Color.Blue ? Color.Blue : Color.Plum);
                    }
                    if (item.HasState(ObjectStatus.AlterWhitespace))
                    {
                        subnode.ForeColor = Color.DarkGoldenrod;
                        NodeColor = (NodeColor == Color.Black || NodeColor == Color.DarkGoldenrod ? Color.DarkGoldenrod : Color.Plum);
                    }
                    if (item.HasState(ObjectStatus.Rebuild))
                    {
                        subnode.ForeColor = Color.Purple;
                        NodeColor = (NodeColor == Color.Black || NodeColor == Color.Purple ? Color.Purple : Color.Plum);
                    }
                    var table = item as Table;
                    if (table != null && MemoryOptimizedTableSql.IsMemoryTable(table))
                    {
                        subnode.ToolTipText = string.Join("\r\n", MemoryOptimizedTableSql.BlockingReasons(table, new List<ISchemaBase>()));
                        if (subnode.ToolTipText.Length > 0) subnode.Text += " [script blocked]";
                    }
                    subnode.Tag = item;
                    subnode.ImageKey = attr.Image;
                    subnode.SelectedImageKey = attr.Image;
                }
            }

            node.ForeColor = NodeColor;
        }

        private void RebuildSchemaTree()
        {
            var checkedSchemas = GetCheckedSchemas();
            string currentlySelectedNode = treeView1.SelectedNode != null ? treeView1.SelectedNode.Name : null;
            string currentTopNode = treeView1.TopNode != null ? treeView1.TopNode.Name : null;

            this.busy = true;
            treeView1.BeginUpdate();
            treeView1.Nodes.Clear();
            TreeNode databaseNode = treeView1.Nodes.Add("root", databaseSource.Name);
            ReadProperties(databaseSource.GetType(), databaseNode.Nodes, databaseSource);
            SetCheckedSchemas(checkedSchemas);
            treeView1.Sort();
            databaseNode.ImageKey = "Database";
            databaseNode.Expand();

            if (currentlySelectedNode != null)
            {
                var nodes = treeView1.Nodes.Find(currentlySelectedNode, true);
                if (nodes.Any()) treeView1.SelectedNode = nodes.First();
            }

            if (currentTopNode != null)
            {
                var nodes = treeView1.Nodes.Find(currentTopNode, true);
                if (nodes.Any()) treeView1.TopNode = nodes.First();
            }

            treeView1.EndUpdate();
            this.busy = false;
            treeView1.Focus();
        }

        private Boolean CanNodeAdd(ISchemaBase item)
        {
            ObjectStatus checkedStatus = ObjectStatus.Original;
            // OriginalStatus == 0, so have to treat differently
            if (item.Status == ObjectStatus.Original && ShowUnchangedItems) return true;

            if (item.HasState(ObjectStatus.Drop) && ShowMissingItems) return true;
            checkedStatus = checkedStatus | ObjectStatus.Drop;

            if (item.HasState(ObjectStatus.Create) && ShowNewItems) return true;
            checkedStatus = checkedStatus | ObjectStatus.Create;

            if (item.HasState(ObjectStatus.Alter) && ShowChangedItems) return true;
            checkedStatus = checkedStatus | ObjectStatus.Alter;

            if (item.HasState(ObjectStatus.AlterWhitespace) && ShowChangedItems) return true;
            checkedStatus = checkedStatus | ObjectStatus.AlterWhitespace;

            if (item.HasState(ObjectStatus.AlterBody) && ShowChangedItems) return true;
            checkedStatus = checkedStatus | ObjectStatus.AlterBody;

            if (item.HasState(ObjectStatus.Rebuild) && ShowChangedItems) return true;
            checkedStatus = checkedStatus | ObjectStatus.Rebuild;

            if (item.HasState(ObjectStatus.RebuildDependencies) && ShowChangedItems) return true;
            checkedStatus = checkedStatus | ObjectStatus.RebuildDependencies;

            if (item.HasState(ObjectStatus.ChangeOwner) && ShowChangedItems) return true;
            checkedStatus = checkedStatus | ObjectStatus.ChangeOwner;

            if (item.HasState(ObjectStatus.DropOlder) && ShowChangedItems) return true;
            checkedStatus = checkedStatus | ObjectStatus.DropOlder;

            if (item.HasState(ObjectStatus.Bind) && ShowChangedItems) return true;
            checkedStatus = checkedStatus | ObjectStatus.Bind;

            if (item.HasState(ObjectStatus.PermissionSet) && ShowChangedItems) return true;
            checkedStatus = checkedStatus | ObjectStatus.PermissionSet;

            if (item.HasState(ObjectStatus.Disabled) && ShowChangedItems) return true;
            checkedStatus = checkedStatus | ObjectStatus.Disabled;

            if (item.HasState(ObjectStatus.Update) && ShowChangedItems) return true;
            checkedStatus = checkedStatus | ObjectStatus.Update;

            // At the end, we should have check all possible statuses.
            ObjectStatus expectedTotalStatus = ObjectStatus.Original;
            Enum.GetValues(typeof(ObjectStatus)).Cast<ObjectStatus>().ToList().ForEach((s) => expectedTotalStatus = expectedTotalStatus | s);

            if (expectedTotalStatus != checkedStatus)
                throw new Exception(string.Format("The OjbectStatusType '{0:G}' wasn't implemented in the CanNodeAdd() method. Developer, please ensure that all values in the Enum are checked.", (ObjectStatus)(expectedTotalStatus - checkedStatus)));

            return false;
        }

        public Boolean ShowNewItems
        {
            get { return chkNew.Checked; }
            set { chkNew.Checked = value; }
        }

        public Boolean ShowMissingItems
        {
            get { return chkOld.Checked; }
            set { chkOld.Checked = value; }
        }

        public Boolean ShowChangedItems
        {
            get { return chkDifferent.Checked; }
            set { chkDifferent.Checked = value; }
        }

        public Boolean ShowUnchangedItems
        {
            get { return chkShowUnchangedItems.Checked; }
            set { chkShowUnchangedItems.Checked = value; }
        }

        private void FilterCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (databaseSource == null) return;

            RebuildSchemaTree();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (busy) return;

            ISchemaBase item = ((ISchemaBase)e.Node.Tag);
            if (item != null)
            {
                if (item.ObjectType == ObjectType.Table
                    || item.ObjectType == ObjectType.View)
                    ReadProperties(item.GetType(), e.Node.Nodes, item);
                if (OnSelectItem != null) OnSelectItem(item.FullName);
            }
        }

        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag == null)
            {
                foreach (TreeNode node in e.Node.Nodes)
                {
                    node.Checked = e.Node.Checked;
                }
            }
        }

        public string SelectedNode
        {
            get
            {
                if (treeView1.SelectedNode == null) return null;

                var item = treeView1.SelectedNode.Tag as ISchemaBase;
                if (item == null) return null;

                return item.FullName;
            }
        }
    }
}
