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
            TreeNode treeNode1 = new TreeNode("ノード3");
            TreeNode treeNode2 = new TreeNode("ノード0", new TreeNode[] { treeNode1 });
            TreeNode treeNode3 = new TreeNode("ノード2");
            TreeNode treeNode4 = new TreeNode("ノード1", new TreeNode[] { treeNode3 });
            treeView1 = new TreeView();
            SuspendLayout();
            // 
            // treeView1
            // 
            treeView1.Dock = DockStyle.Fill;
            treeView1.Location = new Point(0, 0);
            treeView1.Name = "treeView1";
            treeNode1.Name = "ノード3";
            treeNode1.Text = "ノード3";
            treeNode2.Name = "ノード0";
            treeNode2.Text = "ノード0";
            treeNode3.Name = "ノード2";
            treeNode3.Text = "ノード2";
            treeNode4.Name = "ノード1";
            treeNode4.Text = "ノード1";
            treeView1.Nodes.AddRange(new TreeNode[] { treeNode2, treeNode4 });
            treeView1.Size = new Size(800, 450);
            treeView1.TabIndex = 0;
            // 
            // ExplorerTreeForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(treeView1);
            FormBorderStyle = FormBorderStyle.None;
            Name = "ExplorerTreeForm";
            Text = "ExplorerTreeForm";
            ResumeLayout(false);
        }

        #endregion

        private TreeView treeView1;
    }
}