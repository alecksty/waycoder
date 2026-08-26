using WayCoder.Maui.Services;

namespace WayCoder.Maui.Pages;

public partial class FilesPage : ContentPage
{
    /// <summary>当前相对沙箱根的目录（"" = 根）。</summary>
    private string _currentDir = "";

    public FilesPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Refresh();
    }

    private void Refresh()
    {
        FileList.ItemsSource = SandboxFsService.ListDir(_currentDir);
        PathLabel.Text = "/" + _currentDir.TrimStart('/');
    }

    private void OnUpClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentDir)) return; // 已到根
        var idx = _currentDir.TrimEnd('/').LastIndexOf('/');
        _currentDir = idx <= 0 ? "" : _currentDir[..idx];
        Refresh();
    }

    private async void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not SandboxFsService.FsEntry entry) return;
        FileList.SelectedItem = null; // 清选中态，允许再次点同一项

        if (entry.IsDirectory)
        {
            _currentDir = SandboxFsService.ToRelative(entry.FullPath) ?? _currentDir;
            Refresh();
            return;
        }

        // 文件：ActionSheet 打开 / 重命名 / 删除
        var action = await DisplayActionSheetAsync(entry.Name, "取消", null, "打开", "重命名", "删除");
        switch (action)
        {
            case "打开":
                await OpenInEditorAsync(entry);
                break;
            case "重命名":
                await RenameAsync(entry);
                break;
            case "删除":
                await DeleteAsync(entry);
                break;
        }
    }

    private async Task RenameAsync(SandboxFsService.FsEntry entry)
    {
        var rel = SandboxFsService.ToRelative(entry.FullPath) ?? entry.Name;
        var newName = await DisplayPromptAsync("重命名", "输入新名称", accept: "确定", cancel: "取消",
            initialValue: entry.Name, maxLength: 100);
        if (string.IsNullOrWhiteSpace(newName) || newName == entry.Name) return;
        if (SandboxFsService.Rename(rel, newName))
            Refresh();
        else
            await DisplayAlertAsync("重命名失败", "目标名称已存在或路径非法", "关闭");
    }

    private async Task DeleteAsync(SandboxFsService.FsEntry entry)
    {
        var rel = SandboxFsService.ToRelative(entry.FullPath) ?? entry.Name;
        var confirmed = await DisplayAlertAsync("删除确认", $"确定删除「{entry.Name}」？此操作不可撤销。", "删除", "取消");
        if (!confirmed) return;
        if (SandboxFsService.Delete(rel))
            Refresh();
        else
            await DisplayAlertAsync("删除失败", "无法删除该项", "关闭");
    }

    /// <summary>点文件 → 跳内置编辑器（携带沙箱相对路径）。</summary>
    private async Task OpenInEditorAsync(SandboxFsService.FsEntry entry)
    {
        var rel = SandboxFsService.ToRelative(entry.FullPath) ?? entry.Name;
        // 二进制文件（ReadText 返回 null）提示，文本文件进入编辑器
        if (SandboxFsService.ReadText(rel) == null)
        {
            await DisplayAlertAsync("无法打开", $"无法读取 {entry.Name}（可能是二进制文件）", "关闭");
            return;
        }

        await Shell.Current.GoToAsync($"editor?path={Uri.EscapeDataString(rel)}");
    }

    private async void OnImportClicked(object? sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "选择要导入沙箱的文件",
            });
            if (result == null) return;

            var rel = await SandboxFsService.ImportAsync(result);
            Refresh();
            await DisplayAlertAsync("已导入", rel, "确定");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("导入失败", ex.Message, "关闭");
        }
    }
}
