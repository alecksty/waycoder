namespace WayCoder.Maui.Pages;

/// <summary>关于页：图标 / App 名 / 版本号 / 使用说明。</summary>
public partial class AboutPage : ContentPage
{
    public AboutPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        VersionLabel.Text = $"版本 {Global.Version}";
        UsageLabel.Text = Usage;
    }

    private const string Usage = """
        使用说明

        【对话】输入问题与 AI 对话；输入框上方动态状态栏显示思考中/执行工具/等待确认；每轮结束显示用时/token/费用。

        【语音/图片】输入框左侧 ＋：语音输入（录音→转录）、选音频转录、拍照/相册看图（需 vision 模型）。

        【文件】导入项目/文件到沙箱工作区；点文件可打开/用外部应用打开/重命名/删除。

        【编辑器】默认只读防误改，点「✎ 编辑」解锁；左侧行号、长行不换行可横向滚动；markdown 文件点「预览」渲染表格；「保存」写回沙箱。

        【菜单 ☰】右上角：模型选择、模式切换（建造/计划/聊天）、权限切换（Ask/Auto/SmartAuto/Yolo）、会话管理（继续/新会话）、任务管理。

        【会话】退出自动记住对话（仅正文，不含思考/工具结果）；下次进入可「继续会话」或「新的会话」。

        【设置】按服务商填 API Key、选模型。

        【关于】版本与使用说明。
        """;
}
