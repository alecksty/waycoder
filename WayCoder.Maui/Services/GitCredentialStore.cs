using WayCoder.Git;

namespace WayCoder.Maui.Services;

/// <summary>
/// 移动端 git 凭证存储：MAUI SecureStorage 加密保存（跨设备同步用，比 .git/config 明文更安全）。
/// 每次远端操作前把凭证写入 .git/config（<see cref="GitCore.WriteCredential"/>），
/// 让 <see cref="GitRemote"/> 现有 <see cref="GitCore.ReadCredential"/> 读取逻辑不变。
/// </summary>
public static class GitCredentialStore
{
    private const string UserKey = "git_sync_user";
    private const string SecretKey = "git_sync_secret";
    private const string IsTokenKey = "git_sync_is_token";

    public static async Task SaveAsync(string user, string secret, bool isToken = true)
    {
        try
        {
            await SecureStorage.Default.SetAsync(UserKey, user);
            await SecureStorage.Default.SetAsync(SecretKey, secret);
            await SecureStorage.Default.SetAsync(IsTokenKey, isToken ? "1" : "0");
        }
        catch { /* SecureStorage 失败静默：回退 .git/config */ }
    }

    public static async Task<(string User, string Secret, bool IsToken)?> LoadAsync()
    {
        try
        {
            var user = await SecureStorage.Default.GetAsync(UserKey);
            var secret = await SecureStorage.Default.GetAsync(SecretKey);
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(secret)) return null;
            var isToken = await SecureStorage.Default.GetAsync(IsTokenKey) == "1";
            return (user, secret, isToken);
        }
        catch { return null; }
    }

    public static void Clear()
    {
        try
        {
            SecureStorage.Default.Remove(UserKey);
            SecureStorage.Default.Remove(SecretKey);
            SecureStorage.Default.Remove(IsTokenKey);
        }
        catch { }
    }

    /// <summary>把凭证写入仓库 .git/config（GitRemote.ReadCredential 读这里），保证远端操作可用。</summary>
    public static void WriteToRepo(string repoRoot, string user, string secret, bool isToken = true)
    {
        try
        {
            var gitDir = Path.Combine(repoRoot, ".git");
            if (Directory.Exists(gitDir))
                GitCore.WriteCredential(gitDir, user, secret, isToken);
        }
        catch { }
    }
}
