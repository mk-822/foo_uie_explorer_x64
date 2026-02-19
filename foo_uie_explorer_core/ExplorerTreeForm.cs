using foo_uie_explorer_core.MyExplorerLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace foo_uie_explorer_core
{
    public partial class ExplorerTreeForm : Form
    {
        private ImageList imageList = new();
        private ContextMenuStrip contextMenuStrip = new();

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
        }

        private void ExplorerTreeForm_Shown(object sender, EventArgs e)
        {
            // ImageListの初期設定
            imageList.ColorDepth = ColorDepth.Depth32Bit;
            imageList.ImageSize = new Size(16, 16);

            // システムからアイコンをロードして追加
            imageList.Images.Add("folder_closed", SystemIconsExtractor.GetFolderIcon(false));
            imageList.Images.Add("folder_open", SystemIconsExtractor.GetFolderIcon(true));
            imageList.Images.Add("drive", SystemIconsExtractor.GetDriveIcon("C:\\"));
            imageList.Images.Add("favorite", SystemIconsExtractor.GetFavoriteIcon());

            treeView_explorer.ImageList = imageList;

            // 右クリック時にマウス直下のノードを選択状態にするイベントを登録
            treeView_explorer.MouseDown += treeView_explorer_MouseDown;

            // コンテキストメニューの構築
            InitializeContextMenu();

            // ルートノード（お気に入り＋ドライブ）の読み込み
            LoadRootNodes();
        }

        // --- 右クリック時のノード選択処理 ---
        private void treeView_explorer_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                TreeNode clickedNode = treeView_explorer.GetNodeAt(e.X, e.Y);
                if (clickedNode != null)
                {
                    treeView_explorer.SelectedNode = clickedNode;
                }
            }
        }

        // --- お気に入り保存ファイルのパス取得 ---
        private string GetFavoritesFilePath()
        {
            var module = Process.GetCurrentProcess().Modules
                .Cast<ProcessModule>()
                .FirstOrDefault(m => m.ModuleName != null && m.ModuleName.IndexOf("foo_uie_explorer", StringComparison.OrdinalIgnoreCase) >= 0);

            string baseDir = module != null ? Path.GetDirectoryName(module.FileName)! : AppContext.BaseDirectory;
            return Path.Combine(baseDir, "favorites.txt");
        }

        // --- コンテキストメニューの初期化 ---
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

                        // ファイルが存在する場合のみクリアと追加を実行
                        if (files.Length > 0)
                        {
                            string delimitedPaths = string.Join("|", files);
                            ExplorerTree.ClearCurrentPlaylist();
                            ExplorerTree.AddFileToCurrentPlaylist(delimitedPaths);
                        }
                    }
                    catch (UnauthorizedAccessException) { }
                }
            };

            var refreshMenuItem = new ToolStripMenuItem("Refresh");
            refreshMenuItem.Click += (s, args) =>
            {
                if (treeView_explorer.SelectedNode != null)
                {
                    BuildChilds(treeView_explorer.SelectedNode);
                }
                else
                {
                    LoadRootNodes();
                }
            };

            contextMenuStrip.Items.Add(addFavItem);
            contextMenuStrip.Items.Add(removeFavItem);
            contextMenuStrip.Items.Add(new ToolStripSeparator());
            contextMenuStrip.Items.Add(clearAndAddMenuItem);
            contextMenuStrip.Items.Add(new ToolStripSeparator());
            contextMenuStrip.Items.Add(refreshMenuItem);

            contextMenuStrip.Opening += (s, args) =>
            {
                var node = treeView_explorer.SelectedNode;
                if (node == null)
                {
                    addFavItem.Visible = false;
                    removeFavItem.Visible = false;
                    clearAndAddMenuItem.Visible = false;
                    return;
                }

                var type = node.Tag as NodeType?;

                // 表示条件の設定
                addFavItem.Visible = (type == NodeType.Drive || type == NodeType.Folder);
                removeFavItem.Visible = (type == NodeType.Favorite);
                clearAndAddMenuItem.Visible = (type == NodeType.Drive || type == NodeType.Folder || type == NodeType.Favorite);
            };

            treeView_explorer.ContextMenuStrip = contextMenuStrip;
        }

        // --- ルートノード（お気に入り＋ドライブ）の構築 ---
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
                node.ImageKey = node.SelectedImageKey = "drive";
                treeView_explorer.Nodes.Add(node);

                CheckHasFolder(node);
            }
        }

        // --- お気に入りノードの単体追加処理 ---
        private void AddFavoriteNode(string path)
        {
            string title = string.IsNullOrEmpty(Path.GetFileName(path)) ? path : Path.GetFileName(path);

            var node = new TreeNode(title) { Name = path, Tag = NodeType.Favorite };
            node.ImageKey = node.SelectedImageKey = "favorite";

            int insertIndex = treeView_explorer.Nodes.Cast<TreeNode>().Count(n => (n.Tag as NodeType?) == NodeType.Favorite);
            treeView_explorer.Nodes.Insert(insertIndex, node);

            CheckHasFolder(node);
        }

        // --- お気に入りの保存 ---
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
            catch (UnauthorizedAccessException)
            {
                return false;
            }

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
            if (e.Node == null) return;

            try
            {
                string[] files = Directory.GetFiles(e.Node.Name, "*.*", SearchOption.TopDirectoryOnly);

                if (files.Length > 0)
                {
                    string delimitedPaths = string.Join("|", files);

                    // 新しいAPIに差し替え
                    ExplorerTree.AddFileToCurrentPlaylist(delimitedPaths);
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
