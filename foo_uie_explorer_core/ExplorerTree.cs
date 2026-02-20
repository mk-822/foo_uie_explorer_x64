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

        [UnmanagedCallersOnly(EntryPoint = "SetDarkMode")]
        public static void SetDarkMode(bool flag)
        {
            sInstance.SetDarkMode(flag);
        }
    }

    public class FoobarBridge
    {
        //---------------------------------------------------------------------------------
        // プレイリストにファイルを追加するためのデリゲートとメソッド
        //---------------------------------------------------------------------------------
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void OnAddFiles([MarshalAs(UnmanagedType.LPWStr)] string path);
        public static OnAddFiles? sOnAddFile = null;

        [UnmanagedCallersOnly(EntryPoint = "SetOnAddFiles")]
        public static void SetOnAddFiles(IntPtr addPtr)
        {
            if (addPtr != IntPtr.Zero)
            {
                sOnAddFile = Marshal.GetDelegateForFunctionPointer<OnAddFiles>(addPtr);
            }
        }

        public static void AddFiles(string path) => sOnAddFile?.Invoke(path);

        //---------------------------------------------------------------------------------
        // プレイリストをクリアするためのデリゲートとメソッド
        //---------------------------------------------------------------------------------
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void OnClear();
        public static OnClear? sOnClear = null;

        [UnmanagedCallersOnly(EntryPoint = "SetOnClear")]
        public static void SetOnClear(IntPtr addPtr)
        {
            if (addPtr != IntPtr.Zero)
            {
                sOnClear = Marshal.GetDelegateForFunctionPointer<OnClear>(addPtr);
            }
        }

        public static void Clear() => sOnClear?.Invoke();

        //---------------------------------------------------------------------------------
        // フォルダを追加するためのデリゲートとメソッド
        //---------------------------------------------------------------------------------
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void OnAddFolder([MarshalAs(UnmanagedType.LPWStr)] string path);
        public static OnAddFolder? sAddFolder = null;

        [UnmanagedCallersOnly(EntryPoint = "SetOnAddFolder")]
        public static void SetOnAddFolder(IntPtr addPtr)
        {
            if (addPtr != IntPtr.Zero)
            {
                sAddFolder = Marshal.GetDelegateForFunctionPointer<OnAddFolder>(addPtr);
            }
        }

        public static void AddFolder(string path) => sAddFolder?.Invoke(path);
    }
}
