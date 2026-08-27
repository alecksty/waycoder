using WayCoder.Maui.Pages;

namespace WayCoder.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // 编辑器页：非 Tab 路由，从 FilesPage 点文件跳入（携带沙箱相对路径）
        Routing.RegisterRoute("editor", typeof(EditorPage));
        // 关于页：非 Tab 路由，从菜单/首页进入
        Routing.RegisterRoute("about", typeof(AboutPage));
    }
}
