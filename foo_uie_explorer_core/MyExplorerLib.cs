using System;
using System.Collections.Generic;
using System.Text;

namespace foo_uie_explorer_core
{
    using System;
    using System.Drawing;
    using System.Runtime.InteropServices;

    namespace MyExplorerLib
    {
        public static class SystemIconsExtractor
        {
            // --- Win32 API 定義 ---
            private const uint SHGFI_ICON = 0x100;
            private const uint SHGFI_SMALLICON = 0x1;
            private const uint SHGFI_OPENICON = 0x2;
            private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
            private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct SHFILEINFO
            {
                public IntPtr hIcon;
                public int iIcon;
                public uint dwAttributes;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
                public string szDisplayName;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
                public string szTypeName;
            }

            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            private static extern uint ExtractIconEx(string lpszFile, int nIconIndex, out IntPtr phiconLarge, out IntPtr phiconSmall, uint nIcons);

            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool DestroyIcon(IntPtr hIcon);


            // --- アイコン取得メソッド ---

            /// <summary>
            /// 閉じたフォルダ、または開いたフォルダのアイコンを取得します
            /// </summary>
            public static Bitmap GetFolderIcon(bool isOpen)
            {
                uint flags = SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES;
                if (isOpen) flags |= SHGFI_OPENICON;

                SHFILEINFO shfi = new SHFILEINFO();
                // "dummy" という架空のパスとディレクトリ属性を渡すことで、ディスクアクセスせずに標準フォルダアイコンを取得
                SHGetFileInfo("dummy", FILE_ATTRIBUTE_DIRECTORY, ref shfi, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), flags);

                return GetBitmapFromHandleAndDestroy(shfi.hIcon);
            }

            /// <summary>
            /// ドライブのアイコンを取得します（例: "C:\\"）
            /// </summary>
            public static Bitmap GetDriveIcon(string drivePath = "C:\\")
            {
                // 実際のパスのアイコンを調べるため USEFILEATTRIBUTES は付けない
                uint flags = SHGFI_ICON | SHGFI_SMALLICON;

                SHFILEINFO shfi = new SHFILEINFO();
                SHGetFileInfo(drivePath, 0, ref shfi, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), flags);

                return GetBitmapFromHandleAndDestroy(shfi.hIcon);
            }

            /// <summary>
            /// お気に入り（星）アイコンを取得します
            /// </summary>
            public static Bitmap GetFavoriteIcon()
            {
                IntPtr hIconLarge = IntPtr.Zero;
                IntPtr hIconSmall = IntPtr.Zero;

                // shell32.dll のインデックス 43 は昔からある標準の「星」アイコンです
                ExtractIconEx("shell32.dll", 43, out hIconLarge, out hIconSmall, 1);

                // Largeが取得されてしまったら破棄 (今回はSmallだけを使うため)
                if (hIconLarge != IntPtr.Zero) DestroyIcon(hIconLarge);

                return GetBitmapFromHandleAndDestroy(hIconSmall);
            }

            // --- 内部ヘルパー ---

            // ハンドルからBitmapを生成し、不要になったネイティブリソースを確実に解放する
            private static Bitmap GetBitmapFromHandleAndDestroy(IntPtr hIcon)
            {
                if (hIcon == IntPtr.Zero) return new Bitmap(16, 16); // 失敗時は空画像を返す

                try
                {
                    using (Icon icon = Icon.FromHandle(hIcon))
                    {
                        // ToBitmap() で完全にマネージドな画像（GDI+ Bitmap）を作る
                        return icon.ToBitmap();
                    }
                }
                finally
                {
                    // ToBitmap() 後は元の unmanaged アイコンハンドルは不要になるため破棄（超重要）
                    DestroyIcon(hIcon);
                }
            }
        }
    }
}
