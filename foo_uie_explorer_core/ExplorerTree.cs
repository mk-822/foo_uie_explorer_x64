using System.Runtime.InteropServices;

namespace foo_uie_explorer_core
{
    public class ExplorerTree
    {
        public static ExplorerTreeForm sInstance = new(); // インスタンス保持用

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void OnAddFileToCurrentPlaylist([MarshalAs(UnmanagedType.LPWStr)] string path);
        public static OnAddFileToCurrentPlaylist? sOnAddFile = null;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void OnClearCurrentPlaylist();
        public static OnClearCurrentPlaylist? sOnClear = null;

        [UnmanagedCallersOnly(EntryPoint = "Create")]
        public static IntPtr Create()
        {
            sInstance.Show(); // フォームを表示
            return sInstance.Handle;
        }


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

        [UnmanagedCallersOnly(EntryPoint = "SetOnClearCurrentPlaylist")]
        public static void SetOnClearCurrentPlaylist(IntPtr addPtr)
        {
            if (addPtr != IntPtr.Zero)
            {
                sOnClear = Marshal.GetDelegateForFunctionPointer<OnClearCurrentPlaylist>(addPtr);
            }
        }

        public static void ClearCurrentPlaylist()
        {
            sOnClear?.Invoke();
        }
    }
}
