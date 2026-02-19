using System.Runtime.InteropServices;

namespace foo_uie_explorer_core
{
    public class ExplorerTree
    {
        public static ExplorerTreeForm sInstance = new(); // インスタンス保持用
        public static OnAddFileToCurrentPlaylist? sOnAddFile = null;

        [UnmanagedCallersOnly(EntryPoint = "Create")]
        public static IntPtr Create()
        {
            sInstance.Show(); // フォームを表示
            return sInstance.Handle;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void OnAddFileToCurrentPlaylist([MarshalAs(UnmanagedType.LPWStr)] string path);

        // 3. C++から関数ポインタを受け取るためのエクスポート関数
        [UnmanagedCallersOnly(EntryPoint = "SetOnAddFileToCurrentPlaylist")]
        public static void SetOnAddFileToCurrentPlaylist(IntPtr addPtr)
        {
            if (addPtr != IntPtr.Zero)
            {
                sOnAddFile = Marshal.GetDelegateForFunctionPointer<OnAddFileToCurrentPlaylist>(addPtr);
            }
        }

        public static void AddFileToCurrentPlaylist(string path)
        {
            sOnAddFile?.Invoke(path);
        }
    }
}
