namespace foo_uie_explorer_core
{
    partial class ExplorerTreeForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            TreeNode treeNode1 = new TreeNode("Drive", 0, 0);
            TreeNode treeNode2 = new TreeNode("Folder", 2, 2);
            TreeNode treeNode3 = new TreeNode("Favorite", 1, 1);
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExplorerTreeForm));
            treeView_explorer = new TreeView();
            SuspendLayout();
            // 
            // treeView_explorer
            // 
            treeView_explorer.Dock = DockStyle.Fill;
            treeView_explorer.ImageIndex = 0;
            treeView_explorer.ItemHeight = 20;
            treeView_explorer.Location = new Point(0, 0);
            treeView_explorer.Name = "treeView_explorer";
            treeView_explorer.SelectedImageIndex = 0;
            treeView_explorer.Size = new Size(800, 450);
            treeView_explorer.TabIndex = 0;
            treeView_explorer.BeforeExpand += treeView_explorer_BeforeExpand;
            treeView_explorer.AfterExpand += treeView_explorer_AfterExpand;
            treeView_explorer.AfterCollapse += treeView_explorer_AfterCollapse;
            treeView_explorer.NodeMouseDoubleClick += treeView_explorer_NodeMouseDoubleClick;
            // 
            // ExplorerTreeForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(treeView_explorer);
            FormBorderStyle = FormBorderStyle.None;
            Name = "ExplorerTreeForm";
            Text = "ExplorerTreeForm";
            Shown += ExplorerTreeForm_Shown;
            ResumeLayout(false);
        }

        #endregion

        private TreeView treeView_explorer;
    }
}
