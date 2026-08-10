using WayCoder.UI.TuiScreens;

namespace WayCoder.Commands;

public class ModelCommand : SlashCommand
{
    public override string Name => "/model";
    public override string[] Aliases => ["/m"];
    public override string Description => "切换 / 查看模型";
    public override string? Usage => "/model [name]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        if (string.IsNullOrEmpty(args))
        {
            screen.AddSystemMsg($"当前模型: {ProgramContext.Config.Model}\n小模型: {ProgramContext.LLM?.SmallModel ?? "未设置"}");
            return Task.CompletedTask;
        }

        var cfg = ProgramContext.Config;
        var llm = ProgramContext.LLM;
        if (llm == null) { screen.AddSystemMsg("LLM 未初始化"); return Task.CompletedTask; }

        var part = args.Trim();
        if (part.StartsWith("small "))
        {
            llm.SmallModel = part[6..].Trim();
            screen.AddSystemMsg($"小模型已切换: {llm.SmallModel}");
        }
        else
        {
            cfg.Model = part;
            llm.Model = part;
            screen.AddSystemMsg($"模型已切换: {part}");
        }
        return Task.CompletedTask;
    }
}
