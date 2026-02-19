using foo_uie_explorer_core.MyExplorerLib;

namespace foo_uie_explorer_core
{
    public partial class ExplorerTreeForm : Form
    {
        private ImageList imageList;

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
            // ImageListの初期設定 (必要に応じて)
            imageList = new ImageList();
            imageList.ColorDepth = ColorDepth.Depth32Bit;
            imageList.ImageSize = new Size(16, 16);

            // システムからアイコンをロードして追加
            imageList.Images.Add("folder_closed", SystemIconsExtractor.GetFolderIcon(false));
            imageList.Images.Add("folder_open", SystemIconsExtractor.GetFolderIcon(true));
            imageList.Images.Add("drive", SystemIconsExtractor.GetDriveIcon("C:\\"));
            imageList.Images.Add("favorite", SystemIconsExtractor.GetFavoriteIcon());

            // TreeView に紐付け
            treeView_explorer.ImageList = imageList;

            // ルートノードの追加
            treeView_explorer.Nodes.Clear();
            var drives = Environment.GetLogicalDrives();
            foreach (var drive in drives)
            {
                var node = new TreeNode(drive) { Tag = NodeType.Drive };
                treeView_explorer.Nodes.Add(node);
                node.ImageKey = node.SelectedImageKey = "drive";

                CheckHasFolder(node);
            }
        }

        /// <summary>
        /// Determines whether the specified tree node has any associated folders in the file system.
        /// </summary>
        /// <remarks>If one or more folders are found, a dummy node is added to the specified tree node to
        /// indicate that it can be expanded to show its contents.</remarks>
        /// <param name="node">The tree node to check for associated folders. Cannot be null.</param>
        /// <returns>true if the node has one or more associated folders; otherwise, false.</returns>
        private bool CheckHasFolder(TreeNode node)
        {
            if (node == null)
                return false;

            var path = node.FullPath;
            var tag = node.Tag as NodeType?;

            try
            {
                // 隠しフォルダだった場合も探索せず削除する
                if (tag == NodeType.Folder && File.GetAttributes(path).HasFlag(FileAttributes.Hidden))
                    return false;

                var dirs = Directory.GetDirectories(path);

                if (dirs.Length > 0)
                {
                    // フォルダが存在する場合、ダミーノードを追加して展開可能にする
                    var dummy = new TreeNode("Loading...") { Tag = NodeType.Dummy };
                    node.Nodes.Add(dummy);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false; // アクセスが拒否された場合は、フォルダが存在しないとみなす
            }

            return true;
        }

        private void treeView_explorer_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node == null)
                return;
            var type = e.Node.Tag as NodeType?;

            // ダミーノードを削除
            if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Tag is NodeType dummyType && dummyType == NodeType.Dummy)
            {
                e.Node.Nodes.Clear();
                var path = e.Node.FullPath;
                var dirs = Directory.GetDirectories(path);
                foreach (var dir in dirs)
                {
                    var node = new TreeNode(Path.GetFileName(dir)) { Tag = NodeType.Folder };
                    if (type == NodeType.Folder)
                        e.Node.ImageKey = e.Node.SelectedImageKey = "folder_closed";
                    e.Node.Nodes.Add(node);
                    if (!CheckHasFolder(node))
                        e.Node.Nodes.Remove(node);
                }
            }
        }

        private void treeView_explorer_AfterExpand(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null)
                return;

            var type = e.Node.Tag as NodeType?;
            if (type == NodeType.Folder)
                e.Node.ImageKey = e.Node.SelectedImageKey = "folder_closed";
        }

        private void treeView_explorer_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null)
                return;

            var type = e.Node.Tag as NodeType?;
            if (type == NodeType.Folder)
                e.Node.ImageKey = e.Node.SelectedImageKey = "folder_open";
        }

        private void treeView_explorer_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node == null)
                return;

            ExplorerTree.AddFileToCurrentPlaylist(e.Node.FullPath);
        }
    }
}
