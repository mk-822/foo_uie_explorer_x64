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

        // ツリーの開閉直後のダブルクリックを防止するためのタイムスタンプ
        private DateTime _lastExpandCollapseTime = DateTime.MinValue;

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

        private string GetFavoritesFilePath()
        {
            var module = Process.GetCurrentProcess().Modules
                .Cast<ProcessModule>()
                .FirstOrDefault(m => m.ModuleName != null && m.ModuleName.IndexOf("foo_uie_explorer", StringComparison.OrdinalIgnoreCase) >= 0);

            string baseDir = module != null ? Path.GetDirectoryName(module.FileName)! : AppContext.BaseDirectory;
            return Path.Combine(baseDir, "favorites.txt");
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
                    treeView_explorer.Nodes.Remove(treeView_explorer.SelectedNode);
                    SaveFavorites();
                }
            };

            var clearAndAddMenuItem = new ToolStripMenuItem("Clear and Add All Tracks");
            clearAndAddMenuItem.Click += (s, args) =>
            {
                if (treeView_explorer.SelectedNode != null)
                {
                    try
                    {
                        string[] files = Directory.GetFiles(treeView_explorer.SelectedNode.Name, "*.*", SearchOption.TopDirectoryOnly);
                        if (files.Length > 0)
                        {
                            string delimitedPaths = string.Join("|", files);
                            FoobarBridge.Clear();
                            FoobarBridge.AddFiles(delimitedPaths);
                        }
                    }
                    catch (UnauthorizedAccessException) { }
                }
            };

            var addRecursiveMenuItem = new ToolStripMenuItem("Add All Tracks (Recursive)");
            addRecursiveMenuItem.Click += (s, args) =>
            {
                if (treeView_explorer.SelectedNode != null)
                {
                    FoobarBridge.AddFolder(GetPathForFoobar(treeView_explorer.SelectedNode));
                }
            };

            var clearAndAddRecursiveMenuItem = new ToolStripMenuItem("Clear and Add All Tracks (Recursive)");
            clearAndAddRecursiveMenuItem.Click += (s, args) =>
            {
                if (treeView_explorer.SelectedNode != null)
                {
                    FoobarBridge.Clear();
                    FoobarBridge.AddFolder(GetPathForFoobar(treeView_explorer.SelectedNode));
                }
            };

            var refreshMenuItem = new ToolStripMenuItem("Refresh");
            refreshMenuItem.Click += (s, args) =>
            {
                if (treeView_explorer.SelectedNode != null)
                    BuildChilds(treeView_explorer.SelectedNode);
                else
                    LoadRootNodes();
            };

            contextMenuStrip.Items.Add(addFavItem);
            contextMenuStrip.Items.Add(removeFavItem);
            contextMenuStrip.Items.Add(new ToolStripSeparator());
            contextMenuStrip.Items.Add(clearAndAddMenuItem);
            contextMenuStrip.Items.Add(addRecursiveMenuItem);
            contextMenuStrip.Items.Add(clearAndAddRecursiveMenuItem);
            contextMenuStrip.Items.Add(new ToolStripSeparator());
            contextMenuStrip.Items.Add(refreshMenuItem);

            contextMenuStrip.Opening += (s, args) =>
            {
                var node = treeView_explorer.SelectedNode;
                if (node == null)
                {
                    foreach (ToolStripItem item in contextMenuStrip.Items) item.Visible = false;
                    refreshMenuItem.Visible = true;
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

                addRecursiveMenuItem.Visible = (type == NodeType.Drive || type == NodeType.Folder || type == NodeType.Favorite);
                clearAndAddRecursiveMenuItem.Visible = (type == NodeType.Drive || type == NodeType.Folder || type == NodeType.Favorite);
            };

            treeView_explorer.ContextMenuStrip = contextMenuStrip;
        }

        private void LoadRootNodes()
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
                if (tag == NodeType.Folder && File.GetAttributes(path).HasFlag(FileAttributes.Hidden))
                    return false;

                if (Directory.EnumerateDirectories(path).Any())
                {
                    var newNode = new TreeNode("Loading...") { Tag = NodeType.Dummy };
                    node.Nodes.Add(newNode);
                }
            }
            catch (UnauthorizedAccessException) { return false; }
            return true;
        }

        private void treeView_explorer_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            _lastExpandCollapseTime = DateTime.Now;

            if (e.Node == null) return;
            if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Tag is NodeType dummyType && dummyType == NodeType.Dummy)
            {
                BuildChilds(e.Node);
            }
        }

        private void treeView_explorer_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            // 閉じる際もダブルクリック防止タイマーを更新
            _lastExpandCollapseTime = DateTime.Now;
        }

        private void BuildChilds(TreeNode node)
        {
            var type = node.Tag as NodeType?;
            node.Nodes.Clear();
            var path = node.Name;

            try
            {
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
        }

        private void treeView_explorer_AfterExpand(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null) return;
            var type = e.Node.Tag as NodeType?;
            if (type == NodeType.Folder)
                e.Node.ImageKey = e.Node.SelectedImageKey = "folder_open";
        }

        private void treeView_explorer_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null) return;
            var type = e.Node.Tag as NodeType?;
            if (type == NodeType.Folder)
                e.Node.ImageKey = e.Node.SelectedImageKey = "folder_closed";
        }

        private void treeView_explorer_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // 直前にツリーの開閉が行われていた場合はダブルクリックイベントを無視する
            if ((DateTime.Now - _lastExpandCollapseTime).TotalMilliseconds < 300) return;
            if (e.Node == null) return;

            try
            {
                string path = e.Node.Name;

                // CDROM判定
                if ((e.Node.Tag as NodeType?) == NodeType.Drive)
                {
                    DriveInfo di = new DriveInfo(path);
                    if (di.DriveType == DriveType.CDRom)
                    {
                        // foobar2000のCDDAプロトコルとしてフォルダ追加APIに投げる
                        FoobarBridge.AddFolder("cdda://" + path.TrimEnd('\\'));
                        return;
                    }
                }

                // 通常の直下ファイル追加処理
                string[] files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
                if (files.Length > 0)
                {
                    string delimitedPaths = string.Join("|", files);
                    FoobarBridge.AddFiles(delimitedPaths);
                }
            }
            catch (UnauthorizedAccessException) { }
        }
    }
}
