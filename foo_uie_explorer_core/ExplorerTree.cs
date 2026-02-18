using System.Runtime.InteropServices;

namespace foo_uie_explorer_core
{
    public class ExplorerTree
    {
        public static ExplorerTreeForm sInstance = new(); // インスタンス保持用

        [UnmanagedCallersOnly(EntryPoint = "Create")]
        public static IntPtr Create()
        {
            sInstance.Show(); // フォームを表示
            return sInstance.Handle;
        }
    }
}
