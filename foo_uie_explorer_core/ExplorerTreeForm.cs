using foo_uie_explorer_core.MyExplorerLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace foo_uie_explorer_core
{
    public partial class ExplorerTreeForm : Form
    {
        private ImageList imageList = new();
        private ContextMenuStrip contextMenuStrip = new();

        private DoubleClickAction doubleClickAction = DoubleClickAction.Add;
        private bool restoringTree;
        private readonly HashSet<string> expandedNodes = new(StringComparer.OrdinalIgnoreCase);

        private enum DoubleClickAction { ClearAndAdd, Add, ClearAndAddRecursive, AddRecursive }
        private static readonly string[] ActionNames =
        {
            "Clear and Add All Tracks", "Add All Tracks",
            "Clear and Add All Tracks (Recursive)", "Add All Tracks (Recursive)"
        };

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        enum NodeType
        {
            Drive,
            Folder,
            Favorite,
            Dummy,
        }

        public ExplorerTreeForm()
        {
            InitializeComponent();

            treeView_explorer.BorderStyle = BorderStyle.None;
            treeView_explorer.NodeMouseClick += treeView_explorer_NodeMouseClick;

            // 子ウィンドウとして動作させるための設定
            TopLevel = false;
            ShowInTaskbar = false;
        }

        public void SetDarkMode(bool flag)
        {
            if (flag)
            {
                BackColor = Color.FromArgb(30, 30, 30);
                treeView_explorer.BackColor = Color.FromArgb(30, 30, 30);
                treeView_explorer.ForeColor = Color.White;
                contextMenuStrip.BackColor = Color.FromArgb(30, 30, 30);
                contextMenuStrip.ForeColor = Color.White;
            }
            else
            {
                BackColor = SystemColors.Window;
                treeView_explorer.BackColor = SystemColors.Window;
                treeView_explorer.ForeColor = SystemColors.WindowText;
                contextMenuStrip.BackColor = SystemColors.Window;
                contextMenuStrip.ForeColor = SystemColors.WindowText;
            }
        }

        private void ExplorerTreeForm_Shown(object sender, EventArgs e)
        {
            imageList.ColorDepth = ColorDepth.Depth32Bit;
            imageList.ImageSize = new Size(16, 16);

            imageList.Images.Add("folder_closed", SystemIconsExtractor.GetFolderIcon(false));
            imageList.Images.Add("folder_open", SystemIconsExtractor.GetFolderIcon(true));
            imageList.Images.Add("favorite", SystemIconsExtractor.GetFavoriteIcon());
            // ※ドライブアイコンは動的に追加するためここには記述しません

            treeView_explorer.ImageList = imageList;
            treeView_explorer.MouseDown += treeView_explorer_MouseDown;

            InitializeContextMenu();
            LoadSettings();
            LoadRootNodes();
        }

        private void treeView_explorer_MouseDown(object? sender, MouseEventArgs e)
        {
            // 右クリック時にノードを選択する
            if (e.Button == MouseButtons.Right)
            {
                TreeNode? clickedNode = treeView_explorer.GetNodeAt(e.X, e.Y);
                if (clickedNode != null)
                {
                    treeView_explorer.SelectedNode = clickedNode;
                }
            }
        }

        private void treeView_explorer_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            // メディア挿入後は、＋がないドライブもクリックで再検証する。
            if (e.Node != null && (e.Node.Tag as NodeType?) == NodeType.Drive && !e.Node.IsExpanded)
            {
                BuildChilds(e.Node);
            }
        }

        private string GetFavoritesFilePath()
        {
            var module = Process.GetCurrentProcess().Modules
                .Cast<ProcessModule>()
                .FirstOrDefault(m => m.ModuleName != null && m.ModuleName.IndexOf("foo_uie_explorer", StringComparison.OrdinalIgnoreCase) >= 0);

            string baseDir = module != null ? Path.GetDirectoryName(module.FileName)! : AppContext.BaseDirectory;
            return Path.Combine(baseDir, "favorites.txt");
        }

        private string SettingsPath => Path.Combine(Path.GetDirectoryName(GetFavoritesFilePath())!, "explorer-settings.txt");

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return;
                var lines = File.ReadAllLines(SettingsPath);
                if (lines.Length > 0 && int.TryParse(lines[0], out int action) && action >= 0 && action < ActionNames.Length)
                    doubleClickAction = (DoubleClickAction)action;
                expandedNodes.UnionWith(lines.Skip(1));
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private string NodeKey(TreeNode node)
        {
            var root = node;
            while (root.Parent != null) root = root.Parent;
            // Favorites and drives can contain the same path, with independent expansion states.
            return root.Tag + "|" + root.Name + "|" + node.Name;
        }

        private void SaveSettings()
        {
            if (restoringTree) return;
            try
            {
                string path = SettingsPath;
                File.WriteAllLines(path + ".tmp", new[] { ((int)doubleClickAction).ToString() }.Concat(expandedNodes.OrderBy(p => p, StringComparer.OrdinalIgnoreCase)));
                File.Move(path + ".tmp", path, true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private void RestoreExpansion(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                string key = NodeKey(node);
                bool expand = expandedNodes.Contains(key);
                bool hasExpandedDescendant = expandedNodes.Any(p => p.StartsWith(key.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase));
                if (!expand && !hasExpandedDescendant) continue;
                BuildChilds(node);
                RestoreExpansion(node.Nodes);
                if (expand) node.Expand();
            }
        }

        private void ShowConfig()
        {
            using var dialog = new Form
            {
                Text = "Explorer Tree Config", FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent, ClientSize = new Size(400, 130),
                MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false
            };
            var label = new Label { Text = "Double-click action:", AutoSize = true, Location = new Point(12, 15) };
            var actions = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(12, 38), Width = 376 };
            actions.Items.AddRange(ActionNames);
            actions.SelectedIndex = (int)doubleClickAction;
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(222, 88) };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(307, 88) };
            dialog.Controls.AddRange(new Control[] { label, actions, ok, cancel });
            dialog.AcceptButton = ok;
            dialog.CancelButton = cancel;
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                doubleClickAction = (DoubleClickAction)actions.SelectedIndex;
                SaveSettings();
            }
        }

        private void ExecuteTrackAction(TreeNode node, DoubleClickAction action)
        {
            if (node.Tag is not NodeType type || type == NodeType.Dummy) return;
            try
            {
                bool clear = action == DoubleClickAction.ClearAndAdd || action == DoubleClickAction.ClearAndAddRecursive;
                bool recursive = action == DoubleClickAction.ClearAndAddRecursive || action == DoubleClickAction.AddRecursive;
                string path = GetPathForFoobar(node);
                if (recursive || path.StartsWith("cdda://", StringComparison.OrdinalIgnoreCase))
                {
                    if (clear) FoobarBridge.Clear();
                    FoobarBridge.AddFolder(path);
                }
                else
                {
                    string[] files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
                    if (files.Length == 0) return;
                    if (clear) FoobarBridge.Clear();
                    FoobarBridge.AddFiles(string.Join("|", files));
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        // CDDAや通常のパスをfoobar2000向けに変換するヘルパー
        private string GetPathForFoobar(TreeNode node)
        {
            string path = node.Name;
            if ((node.Tag as NodeType?) == NodeType.Drive)
            {
                try
                {
                    DriveInfo di = new DriveInfo(path);
                    if (di.DriveType == DriveType.CDRom)
                    {
                        return "cdda://" + path.TrimEnd('\\');
                    }
                }
                catch { }
            }
            return path;
        }

        private void InitializeContextMenu()
        {
            var showInExplorer = new ToolStripMenuItem("Show in Explorer");
            showInExplorer.Click += (s, args) =>
            {
                if (treeView_explorer.SelectedNode != null)
                {
                    string path = treeView_explorer.SelectedNode.Name;
                    if (Directory.Exists(path) || File.Exists(path))
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = path,
                                UseShellExecute = true,
                                Verb = "open"
                            });
                        }
                        catch { }
                    }
                }
            };

            var addFavItem = new ToolStripMenuItem("Add to Favorites");
            addFavItem.Click += (s, args) =>
            {
                if (treeView_explorer.SelectedNode != null)
                {
                    string path = treeView_explorer.SelectedNode.Name;
                    if (!treeView_explorer.Nodes.Cast<TreeNode>().Any(n => (n.Tag as NodeType?) == NodeType.Favorite && n.Name.Equals(path, StringComparison.OrdinalIgnoreCase)))
                    {
                        AddFavoriteNode(path);
                        SaveFavorites();
                    }
                }
            };

            var removeFavItem = new ToolStripMenuItem("Remove from Favorites");
            removeFavItem.Click += (s, args) =>
            {
                if (treeView_explorer.SelectedNode != null && (treeView_explorer.SelectedNode.Tag as NodeType?) == NodeType.Favorite)
                {
                    string key = NodeKey(treeView_explorer.SelectedNode);
                    expandedNodes.RemoveWhere(p => p.Equals(key, StringComparison.OrdinalIgnoreCase) || p.StartsWith(key.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase));
                    treeView_explorer.Nodes.Remove(treeView_explorer.SelectedNode);
                    SaveSettings();
                    SaveFavorites();
                }
            };

            var clearAndAddMenuItem = new ToolStripMenuItem(ActionNames[0]);
            clearAndAddMenuItem.Click += (s, args) => RunSelectedAction(DoubleClickAction.ClearAndAdd);
            var addMenuItem = new ToolStripMenuItem(ActionNames[1]);
            addMenuItem.Click += (s, args) => RunSelectedAction(DoubleClickAction.Add);
            var clearAndAddRecursiveMenuItem = new ToolStripMenuItem(ActionNames[2]);
            clearAndAddRecursiveMenuItem.Click += (s, args) => RunSelectedAction(DoubleClickAction.ClearAndAddRecursive);
            var addRecursiveMenuItem = new ToolStripMenuItem(ActionNames[3]);
            addRecursiveMenuItem.Click += (s, args) => RunSelectedAction(DoubleClickAction.AddRecursive);
            var configMenuItem = new ToolStripMenuItem("Config...");
            configMenuItem.Click += (s, args) => ShowConfig();

            var refreshMenuItem = new ToolStripMenuItem("Refresh");
            refreshMenuItem.Click += (s, args) =>
            {
                if (treeView_explorer.SelectedNode != null)
                {
                    restoringTree = true;
                    try
                    {
                        var node = treeView_explorer.SelectedNode;
                        BuildChilds(node);
                        RestoreExpansion(node.Nodes);
                        if (expandedNodes.Contains(NodeKey(node))) node.Expand();
                    }
                    finally { restoringTree = false; }
                }
                else
                    LoadRootNodes();
            };

            contextMenuStrip.Items.Add(showInExplorer);
            contextMenuStrip.Items.Add(new ToolStripSeparator());
            contextMenuStrip.Items.Add(addFavItem);
            contextMenuStrip.Items.Add(removeFavItem);
            contextMenuStrip.Items.Add(new ToolStripSeparator());
            contextMenuStrip.Items.Add(clearAndAddMenuItem);
            contextMenuStrip.Items.Add(addMenuItem);
            contextMenuStrip.Items.Add(addRecursiveMenuItem);
            contextMenuStrip.Items.Add(clearAndAddRecursiveMenuItem);
            contextMenuStrip.Items.Add(new ToolStripSeparator());
            contextMenuStrip.Items.Add(refreshMenuItem);
            contextMenuStrip.Items.Add(configMenuItem);

            contextMenuStrip.Opening += (s, args) =>
            {
                foreach (ToolStripItem item in contextMenuStrip.Items) item.Visible = true;
                var node = treeView_explorer.SelectedNode;
                if (node == null)
                {
                    foreach (ToolStripItem item in contextMenuStrip.Items) item.Visible = false;
                    refreshMenuItem.Visible = true;
                    configMenuItem.Visible = true;
                    return;
                }

                var type = node.Tag as NodeType?;

                addFavItem.Visible = (type == NodeType.Drive || type == NodeType.Folder);
                removeFavItem.Visible = (type == NodeType.Favorite);

                // CDROM等の判定
                bool isDrive = type == NodeType.Drive;
                bool isCdRom = false;
                if (isDrive)
                {
                    try { isCdRom = new DriveInfo(node.Name).DriveType == DriveType.CDRom; } catch { }
                }

                // 階層を持たないCDROMの場合はNon-Recursive系のメニューを隠す
                clearAndAddMenuItem.Visible = !isCdRom && (type == NodeType.Drive || type == NodeType.Folder || type == NodeType.Favorite);

                addMenuItem.Visible = clearAndAddMenuItem.Visible;
                addRecursiveMenuItem.Visible = (type == NodeType.Drive || type == NodeType.Folder || type == NodeType.Favorite);
                clearAndAddRecursiveMenuItem.Visible = (type == NodeType.Drive || type == NodeType.Folder || type == NodeType.Favorite);
            };

            treeView_explorer.ContextMenuStrip = contextMenuStrip;
        }

        private void LoadRootNodes()
        {
            restoringTree = true;
            treeView_explorer.BeginUpdate();
            try
            {
                treeView_explorer.Nodes.Clear();

                string favPath = GetFavoritesFilePath();
                if (File.Exists(favPath))
                {
                    var savedPaths = File.ReadAllLines(favPath).Where(p => Directory.Exists(p));
                    foreach (var path in savedPaths)
                    {
                        AddFavoriteNode(path);
                    }
                }

                var drives = Environment.GetLogicalDrives();
                foreach (var drive in drives)
                {
                    var node = new TreeNode(drive) { Name = drive, Tag = NodeType.Drive };

                    // ドライブ固有のアイコンを取得してImageListに登録
                    string iconKey = "drive_" + drive;
                    if (!imageList.Images.ContainsKey(iconKey))
                    {
                        imageList.Images.Add(iconKey, SystemIconsExtractor.GetDriveIcon(drive));
                    }
                    node.ImageKey = node.SelectedImageKey = iconKey;

                    treeView_explorer.Nodes.Add(node);
                    CheckHasFolder(node);
                }
                RestoreExpansion(treeView_explorer.Nodes);
            }
            finally
            {
                treeView_explorer.EndUpdate();
                restoringTree = false;
            }
        }

        private void AddFavoriteNode(string path)
        {
            string title = string.IsNullOrEmpty(Path.GetFileName(path)) ? path : Path.GetFileName(path);
            var node = new TreeNode(title) { Name = path, Tag = NodeType.Favorite };
            node.ImageKey = node.SelectedImageKey = "favorite";

            int insertIndex = treeView_explorer.Nodes.Cast<TreeNode>().Count(n => (n.Tag as NodeType?) == NodeType.Favorite);
            treeView_explorer.Nodes.Insert(insertIndex, node);
            CheckHasFolder(node);
        }

        private void SaveFavorites()
        {
            var favNodes = treeView_explorer.Nodes.Cast<TreeNode>().Where(n => (n.Tag as NodeType?) == NodeType.Favorite);
            var paths = favNodes.Select(n => n.Name).ToArray();
            File.WriteAllLines(GetFavoritesFilePath(), paths);
        }

        private bool CheckHasFolder(TreeNode node)
        {
            if (node == null) return false;
            var path = node.Name;
            var tag = node.Tag as NodeType?;

            try
            {
                if (tag == NodeType.Drive && !new DriveInfo(path).IsReady)
                    return false;

                if (tag == NodeType.Folder && File.GetAttributes(path).HasFlag(FileAttributes.Hidden))
                    return false;

                if (Directory.EnumerateDirectories(path).Any())
                {
                    var newNode = new TreeNode("Loading...") { Tag = NodeType.Dummy };
                    node.Nodes.Add(newNode);
                }
            }
            catch (UnauthorizedAccessException) { return false; }
            catch (IOException) { return false; }
            return true;
        }

        private void treeView_explorer_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node == null) return;
            if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Tag is NodeType dummyType && dummyType == NodeType.Dummy)
            {
                BuildChilds(e.Node);
            }
        }

        private void BuildChilds(TreeNode node)
        {
            var type = node.Tag as NodeType?;
            node.Nodes.Clear();
            var path = node.Name;

            try
            {
                if (type == NodeType.Drive && !new DriveInfo(path).IsReady)
                    return;

                var dirs = Directory.GetDirectories(path);
                foreach (var dir in dirs)
                {
                    var newNode = new TreeNode(Path.GetFileName(dir)) { Name = dir, Tag = NodeType.Folder };

                    if (type == NodeType.Folder || type == NodeType.Favorite || type == NodeType.Drive)
                    {
                        newNode.ImageKey = newNode.SelectedImageKey = "folder_closed";
                    }
                    node.Nodes.Add(newNode);

                    if (!CheckHasFolder(newNode))
                    {
                        node.Nodes.Remove(newNode);
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }

        private void treeView_explorer_AfterExpand(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null) return;
            var type = e.Node.Tag as NodeType?;
            if (type == NodeType.Folder)
                e.Node.ImageKey = e.Node.SelectedImageKey = "folder_open";
            if (!restoringTree)
            {
                expandedNodes.Add(NodeKey(e.Node));
                SaveSettings();
            }
        }

        private void treeView_explorer_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null) return;
            var type = e.Node.Tag as NodeType?;
            if (type == NodeType.Folder)
                e.Node.ImageKey = e.Node.SelectedImageKey = "folder_closed";
            if (!restoringTree)
            {
                expandedNodes.Remove(NodeKey(e.Node));
                SaveSettings();
            }
        }

        private void RunSelectedAction(DoubleClickAction action)
        {
            if (treeView_explorer.SelectedNode is TreeNode node) ExecuteTrackAction(node, action);
        }

        private void treeView_explorer_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Node != null) ExecuteTrackAction(e.Node, doubleClickAction);
        }
    }

    // Native TreeView toggles folders before raising its double-click event.
    // Handle label/icon double-clicks directly so playlist actions do not also toggle the tree.
    internal sealed class ExplorerTreeView : TreeView
    {
        protected override void WndProc(ref Message m)
        {
            const int WM_LBUTTONDBLCLK = 0x0203;
            if (m.Msg == WM_LBUTTONDBLCLK)
            {
                int x = unchecked((short)(m.LParam.ToInt64() & 0xffff));
                int y = unchecked((short)((m.LParam.ToInt64() >> 16) & 0xffff));
                var hit = HitTest(x, y);
                if (hit.Node != null && (hit.Location & (TreeViewHitTestLocations.Label | TreeViewHitTestLocations.Image)) != 0)
                {
                    SelectedNode = hit.Node;
                    OnNodeMouseDoubleClick(new TreeNodeMouseClickEventArgs(hit.Node, MouseButtons.Left, 2, x, y));
                    m.Result = IntPtr.Zero;
                    return;
                }
            }
            base.WndProc(ref m);
        }
    }
}
