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
        // 供应商与模型管理页：从菜单/首页进入
        Routing.RegisterRoute("models", typeof(ModelManagerPage));
        // 模型选择对话框（TUI ModelPicker 移植）：从模型条/菜单进入
        Routing.RegisterRoute("modelpicker", typeof(ModelPickerPage));
        // 供应商模型列表详情页：从供应商/模型管理页点供应商滑入
        Routing.RegisterRoute("providermodels", typeof(ProviderModelsPage));
    }
}
