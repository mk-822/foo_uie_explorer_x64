/**
 * \file main.cpp
 *
 * \brief Example panel component
 *
 * This component is an example of a very simple multiple instance panel that does not take any keyboard input.
 */

// Included before windows.h, because pfc.h includes winsock2.h
#include "../pfc/pfc.h"

#include <windows.h>
#include <windowsx.h>
#include <commctrl.h>

#include "../foobar2000/SDK/foobar2000.h"

#include "../columns_ui-sdk/ui_extension.h"

/** Declare some information about our component */
DECLARE_COMPONENT_VERSION("Example Columns UI Panel", "0.1", "compiled: " __DATE__);

/**
 * The GUID that identifies our panel.
 *
 * You must change this – don't use the same one!
 */
constexpr GUID extension_guid = {0x97ee6584, 0x7fb1, 0x48e9, {0xbf, 0xaa, 0xdc, 0xf3, 0x2, 0x95, 0x9a, 0xcf}};

HWND wnd_fb2k{nullptr};
typedef void(__stdcall* RegisterCallbacksFunc)(void*);

    // アクティブなプレイリストに複数ファイルを追加 (パイプ '|' 区切りの文字列を受け取る)
void __stdcall Host_AddFiles(const wchar_t* delimited_paths)
{
    if (!delimited_paths || wcslen(delimited_paths) == 0)
        return;

    static_api_ptr_t<playlist_manager> pm;
    t_size active = pm->get_active_playlist();

    if (active != pfc::infinite_size) {
        pfc::list_t<pfc::string8> location_strings;
        pfc::list_t<const char*> locations;

        std::wstring paths(delimited_paths);
        size_t start = 0;
        size_t end = paths.find(L'|');

        // パスを判定してリストに追加する処理（ラムダ式で共通化）
        auto check_and_add = [&](const std::wstring& path_str) {
            auto utf8_path = pfc::stringcvt::string_utf8_from_wide(path_str.c_str());

            // 1. 音楽ファイルとして対応しているか判定 (.mp3, .flac, .dsf などインストール済みコンポーネントに依存)
            bool is_supported = input_entry::g_is_supported_path(utf8_path);

            // 2. プレイリストファイルとして対応しているか判定 (.cue, .m3u など)
            //if (!is_supported) {
            //    is_supported = playlist_loader::g_is_supported_path(utf8_path);
            //}

            // 対応している場合のみ追加
            if (is_supported) {
                location_strings.add_item(utf8_path);
            }
        };

        // パイプ文字で分割しながら判定処理を実行
        while (end != std::wstring::npos) {
            check_and_add(paths.substr(start, end - start));
            start = end + 1;
            end = paths.find(L'|', start);
        }
        // 最後の1要素を処理
        if (start < paths.length()) {
            check_and_add(paths.substr(start));
        }

        // 1つ以上対応ファイルがあった場合のみ、プレイリストへの一括追加を実行
        if (location_strings.get_count() > 0) {
            for (t_size i = 0; i < location_strings.get_count(); ++i) {
                locations.add_item(location_strings[i].get_ptr());
            }
            pm->playlist_add_locations(active, locations, true, wnd_fb2k);
        }
    }
}

// アクティブなプレイリストをクリア
void __stdcall Host_ClearPlaylist()
{
    // メインスレッドからの呼び出しを想定
    static_api_ptr_t<playlist_manager> pm;
    t_size active = pm->get_active_playlist();

    if (active != pfc::infinite_size) {
        // undo_backup を true にすると、ユーザーが「元に戻す」を使えるようになります
        pm->playlist_clear(active);
    }
}

/** Our window class. */
class ExampleWindow : public uie::container_ui_extension {
public:
    const GUID& get_extension_guid() const override { return extension_guid; }
    void get_name(pfc::string_base& out) const override { out = "Example"; }
    void get_category(pfc::string_base& out) const override { out = "Panels"; }
    unsigned get_type() const override { return uie::type_panel; }

private:
    class_data& get_class_data() const override
    {
        __implement_get_class_data(_T("{97EE6584-7FB1-48E9-BFAA-DCF302959ACF}"), true);
    }

    /** Our window procedure */
    LRESULT on_message(HWND wnd, UINT msg, WPARAM wp, LPARAM lp) override;
    void get_menu_items(uie::menu_hook_t& p_hook) override;

    /** Our child window */
    HWND wnd_static{nullptr};
    HMODULE hDll;

    void LoadLibraryImpl()
    {
        pfc::string8 my_path = core_api::get_my_full_path();

        // 例: "C:\foobar2000\user-components\foo_uie_example\foo_uie_example.dll"

        // 2. ファイル名部分を取り除き、ディレクトリパスにする
        // scan_filename() は最後の '\\' または '/' の位置を返します
        t_size name_start = my_path.scan_filename();
        my_path.truncate(name_start);

        // 3. 読み込みたい DLL 名を結合
        my_path += "foo_uie_explorer_core.dll";

        // 4. LoadLibrary 用に UTF-8 (pfc::string8) から WideChar (wchar_t*) に変換
        pfc::stringcvt::string_wide_from_utf8 wide_path(my_path);

        // 5. DLL をロード
        // ★重要: LOAD_WITH_ALTERED_SEARCH_PATH を使う
        // これを使わないと、MyExplorerLib.dll がさらに別の DLL に依存している場合、
        // foobar2000.exe の場所を探しに行ってしまい失敗することがあります。
        hDll = LoadLibraryEx(wide_path, nullptr, LOAD_WITH_ALTERED_SEARCH_PATH);
    }
};

void ExampleWindow::get_menu_items(uie::menu_hook_t& p_hook)
{
    const uie::menu_node_ptr close_panel = new uie::simple_command_menu_node(
        "Close panel", "Removes this panel from the layout.", 0, [self = service_ptr_t<ExampleWindow>(this)] {
            const HWND wnd = self->get_wnd();
            uie::window_host_ptr host = self->get_host();
            uie::window_ptr(self)->destroy_window();
            host->relinquish_ownership(wnd);
        });
    p_hook.add_node(close_panel);
}

LRESULT ExampleWindow::on_message(HWND wnd, UINT msg, WPARAM wp, LPARAM lp)
{
    switch (msg) {
    case WM_CREATE: {
        LoadLibraryImpl();
        RECT rc;
        GetClientRect(wnd, &rc);
        wnd_fb2k = wnd;

        /** Create a static window, with text "Example". */
        if (hDll) {
            auto method = GetProcAddress(hDll, "Create");
            // 3. C#の関数を呼ぶ
            if (method) {
                wnd_static = (HWND)method();
                SetParent(wnd_static, wnd);
            }

            auto regFunc = (RegisterCallbacksFunc)GetProcAddress(hDll, "SetOnAddFileToCurrentPlaylist");
            if (regFunc) {
                // 作成した関数のアドレスを渡す
                regFunc((void*)Host_AddFiles);
            }

            regFunc = (RegisterCallbacksFunc)GetProcAddress(hDll, "SetOnClearCurrentPlaylist");
            if (regFunc) {
                // 作成した関数のアドレスを渡す
                regFunc((void*)Host_ClearPlaylist);
            }
        }
        //wnd_static = CreateWindowEx(0, WC_STATIC, _T("Example panel"), WS_CHILD | WS_VISIBLE, 0, 0, rc.right, rc.bottom,
        //    wnd, HMENU(0), core_api::get_my_instance(), nullptr);
    } break;
    case WM_SIZE:
        /** The static control requires this to redraw correctly. */
        RedrawWindow(wnd_static, 0, 0, RDW_INVALIDATE | RDW_ERASE);
        /** Reposition the static control. */
        SetWindowPos(wnd_static, 0, 0, 0, LOWORD(lp), HIWORD(lp), SWP_NOZORDER);
        break;
    case WM_DESTROY:
        /** DefWindowProc will destroy our child window. Set our window handle to nullptr now. */
        wnd_static = nullptr;

        if (hDll) {
            FreeLibrary(hDll);
        }
        break;
    default:
        break;
    }
    return DefWindowProc(wnd, msg, wp, lp);
}

static uie::window_factory<ExampleWindow> example_window_factory;
