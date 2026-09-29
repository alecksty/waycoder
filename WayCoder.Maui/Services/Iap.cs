#if ANDROID
using Android.BillingClient.Api;
#endif

namespace WayCoder.Maui.Services;

/// <summary>
/// 一次「购买 / 恢复购买」操作的结果。
///
/// <para>
/// ⚠ <b><paramref name="Ok"/> 说的是"这次操作本身成没成功"，不是"现在有没有解锁"</b> ——
/// 两者必须分开看。典型反例：<b>点「恢复购买」而该 Apple ID 一件都没买过</b> ——
/// 操作<em>成功</em>了（商店答得很清楚："没有可恢复的购买"），但资格是**没有**。
/// 所以调完一律读 <see cref="EntitlementStore.IsFull"/> 拿真值，别拿 <see cref="Ok"/> 当解锁判据。
/// </para>
/// </summary>
/// <param name="Ok">本次操作是否正常完成（用户主动取消、网络失败、商店没配好 ⇒ false）。</param>
/// <param name="Message">给用户看的**一整句话**（已本地化，可直接上屏）。</param>
public sealed record IapResult(bool Ok, string Message);

/// <summary>
/// <b>内购的平台层</b> —— 唯一会调 <see cref="EntitlementStore.Set"/> 的地方。
///
/// <para>
/// <b>为什么两端各写一份、不抽共享实现</b>：StoreKit 与 Google Play Billing 的产品模型、
/// 交易回调、恢复语义全都不一样，抽一层接口只是把 <c>#if</c> 挪个地方，还多一层要同步的
/// 平行表（本仓头号坑）。这与 <c>VmlAudio</c> 的处置一致。
/// </para>
///
/// <para>
/// <b>为什么不用 <c>Plugin.InAppBilling</c> 之类的跨端插件</b>：
/// ① 本仓硬约束「iOS 是 full AOT、禁运行时反射」，而 IAP 插件里打包的是自己的
///    StoreKit/Billing 封装，**吃不吃反射只能等发布包在真机上崩才知道**（Debug 下看不出来）；
/// ② iOS 的 StoreKit 绑定**随 SDK 一起就有**（`StoreKit.SKPaymentQueue` 等），零依赖、天然 AOT 安全；
/// ③ 代码量本来就小（这一版两个平台各约一百多行），不值得为省这点引入一个不可控的第三方。
/// </para>
///
/// <para>
/// ⚠ <b>本文件的平台分支只做了「编译验证」</b>：StoreKit 与 Play Billing 的真实交易
/// 需要真机 + 商店后台建好产品才能跑通，这些在开发机上做不到。
/// 正式上架前必须走一遍真机沙箱（App Store Connect 的 Sandbox 测试账号 / Play 的
/// 许可测试账号），判据见 <c>docs/上架资料包.md</c> 第 8.3 节。
/// </para>
/// </summary>
internal static class Iap
{
    /// <summary>
    /// 当前平台**有没有**内购实现（不等于"商店可用"—— 那要连上商店才知道）。
    /// 设置页据此决定是显示购买按钮还是"本平台不支持"。
    /// </summary>
    public static bool Supported =>
#if IOS || MACCATALYST || ANDROID
        true;
#else
        false;
#endif

    // ── 平台实现（`#if` 三分支；桌面/Windows 那份是显式的"不支持"，不是留空）──

#if IOS || MACCATALYST

    private static StoreKit.SKPaymentQueue? _queue;
    private static TransactionObserver? _observer;
    private static ProductsDelegate? _products;

    /// <summary>正在等的那次操作（购买或恢复）。同一时刻只允许一次（见 <see cref="Begin"/>）。</summary>
    private static TaskCompletionSource<IapResult>? _pending;

    /// <summary>上一次「恢复购买」是否**成功**完成过。用来区分"商店说没有"与"商店没答上来"。</summary>
    private static bool _restoreSawTransaction;

    /// <summary>
    /// App 启动时调一次（幂等）。**必须在启动早期调** —— StoreKit 要求在 App 起来后尽快
    /// 挂上交易观察者，否则**上一次杀掉 App 时没走完的交易**（比如付款成功但 App 被杀了）
    /// 不会送到这里，用户会看到"钱扣了、没解锁"。
    /// </summary>
    public static void Start()
    {
        if (_queue is not null) return;
        try
        {
            _observer = new TransactionObserver();
            _queue = StoreKit.SKPaymentQueue.DefaultQueue;
            _queue.AddTransactionObserver(_observer);

            // 产品信息是**异步**取的，只为拿到本地化价格显示在按钮上；
            // 取不到不影响购买（`SKMutablePayment.PaymentWithProduct(string)` 只认 ID）。
            _products = new ProductsDelegate();
            using var ids = new Foundation.NSSet<Foundation.NSString>(
                new Foundation.NSString(EntitlementStore.ProductId));
            var req = new StoreKit.SKProductsRequest(ids);
            req.Delegate = _products;
            req.Start();
        }
        catch (Exception ex)
        {
            ErrorLog.Warning("[IAP]", $"Start 失败：{ex.Message}");
        }
    }

    /// <summary>商店返回的**本地化价格**（"¥68.00" 这种，含币种）。没取到就是 null。</summary>
    public static string? PriceText => _products?.Price;

    /// <summary>
    /// 发起购买。**先建 TCS 再下单** —— 反过来的话，交易回调可能先于 TCS 建好而到，
    /// 结果就是"扣了钱、界面一直转圈"。
    /// </summary>
    public static Task<IapResult> PurchaseAsync(string productId)
    {
        var gate = Begin();
        if (gate is not null) return Task.FromResult(gate);

        try
        {
            _pending = new TaskCompletionSource<IapResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var payment = StoreKit.SKMutablePayment.PaymentWithProduct(productId);
            _queue!.AddPayment(payment);
            return _pending.Task;
        }
        catch (Exception ex)
        {
            return Task.FromResult(End(false, L.Pick($"⚠️ 无法发起购买：{ex.Message}", $"⚠️ Could not start the purchase: {ex.Message}")));
        }
    }

    /// <summary>
    /// 恢复购买。**这是上架硬要求**（非消耗型必须能跨设备/重装恢复，审核会实际点它）。
    ///
    /// <para>
    /// ⚠ 三件事的区分是这个函数唯一的难点：**恢复成功但没有可恢复项**（⇒ 资格是没有的，
    /// 要把本地状态写成 false）、**恢复本身失败**（网络/未登录 ⇒ 本地状态一个字都不能动）、
    /// **恢复到了**（⇒ true）。写成"没到就 false"会让断网的用户一按恢复就丢掉已买的资格。
    /// </para>
    /// </summary>
    public static Task<IapResult> RestoreAsync()
    {
        var gate = Begin();
        if (gate is not null) return Task.FromResult(gate);

        try
        {
            _restoreSawTransaction = false;
            _pending = new TaskCompletionSource<IapResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _queue!.RestoreCompletedTransactions();
            return _pending.Task;
        }
        catch (Exception ex)
        {
            return Task.FromResult(End(false, L.Pick($"⚠️ 无法发起恢复购买：{ex.Message}", $"⚠️ Could not start the restore: {ex.Message}")));
        }
    }

    /// <summary>同一时刻只允许一次内购操作（连点两下购买会弹两次系统支付框）。</summary>
    private static IapResult? Begin()
        => _pending is not null
            ? new IapResult(false, L.Pick("⏳ 上一次操作还在进行中，请稍候。", "⏳ The previous purchase is still in progress."))
            : null;

    private static IapResult End(bool ok, string message)
    {
        var tcs = _pending;
        _pending = null;
        tcs?.TrySetResult(new IapResult(ok, message));
        return new IapResult(ok, message);
    }

    /// <summary>
    /// 交易观察者。**它才是解锁的真入口** —— 购买、恢复、以及"上次没走完的交易"全都从这里进来，
    /// <see cref="PurchaseAsync"/> 那条路只是"下单 + 等结果"。
    /// </summary>
    private sealed class TransactionObserver : StoreKit.SKPaymentTransactionObserver
    {
        public override void UpdatedTransactions(StoreKit.SKPaymentQueue queue,
                                                 StoreKit.SKPaymentTransaction[] transactions)
        {
            foreach (var tx in transactions)
            {
                // 只认我们自己的产品 —— 观察者是**全局**的，将来加第二个产品时
                // 漏了这个判据就会"买了别的东西把这一个也解锁了"。
                bool mine = tx.Payment?.ProductIdentifier == EntitlementStore.ProductId;

                switch (tx.TransactionState)
                {
                    case StoreKit.SKPaymentTransactionState.Purchased:
                    case StoreKit.SKPaymentTransactionState.Restored:
                        if (mine) EntitlementStore.Set(true);
                        _restoreSawTransaction |= mine;
                        queue.FinishTransaction(tx);   // ⚠ 必须收尾，否则这笔交易每次启动都会重放
                        if (_pending is not null)
                            End(true, tx.TransactionState == StoreKit.SKPaymentTransactionState.Restored
                                ? L.Pick("✅ 已恢复购买。", "✅ Purchase restored.")
                                : L.Pick("✅ 已解锁全能版，谢谢支持！", "✅ The Full Edition is unlocked - thank you!"));
                        break;

                    case StoreKit.SKPaymentTransactionState.Failed:
                        queue.FinishTransaction(tx);
                        if (_pending is not null)
                            End(false, DescribeFailure(tx.Error));
                        break;

                    case StoreKit.SKPaymentTransactionState.Deferred:
                        // 家人共享 / 家长控制下等批准：**不能 FinishTransaction**（交易还没结束）
                        if (_pending is not null)
                            End(false, L.Pick("⏳ 这笔购买正在等待批准（家长控制 / 家人共享），批准后会自动解锁。",
                                              "⏳ This purchase is waiting for approval (parental controls / Family Sharing); it unlocks once approved."));
                        break;

                    // Purchasing：什么都不做，等下一轮回调
                }
            }
        }

        public override void RestoreCompletedTransactionsFinished(StoreKit.SKPaymentQueue queue)
        {
            if (_pending is null) return;
            // 恢复过程走完、一条 OUR 产品的交易都没见到 ⇒ **这个账号确实没有资格**。
            // 只有走到这里才敢写 false —— 这是"只增不减"的唯一例外，也是退款/换号后能收敛的原因。
            if (!_restoreSawTransaction) EntitlementStore.Set(false);
            End(true, _restoreSawTransaction
                ? L.Pick("✅ 已恢复购买。", "✅ Purchase restored.")
                : L.Pick("没有找到可恢复的购买记录。", "No previous purchase was found to restore."));
        }

        public override void RestoreCompletedTransactionsFailedWithError(StoreKit.SKPaymentQueue queue,
                                                                        Foundation.NSError error)
        {
            // ⚠ 这里**绝对不能**动解锁状态：失败的是"这次查询"，不是"这笔购买"。
            End(false, L.Pick($"⚠️ 恢复购买失败：{error.LocalizedDescription}",
                              $"⚠️ Restore failed: {error.LocalizedDescription}"));
        }

        /// <summary>App Store 商品页上的「App 内购买」直达入口（用户从商店页点进来）。</summary>
        public override bool ShouldAddStorePayment(StoreKit.SKPaymentQueue queue,
                                                   StoreKit.SKPayment payment,
                                                   StoreKit.SKProduct product)
            => payment.ProductIdentifier == EntitlementStore.ProductId;

        private static string DescribeFailure(Foundation.NSError? error)
        {
            if (error is not null && (StoreKit.SKError)error.Code == StoreKit.SKError.PaymentCancelled)
                return L.Pick("已取消购买。", "Purchase cancelled.");
            return error is null
                ? L.Pick("⚠️ 购买失败。", "⚠️ The purchase failed.")
                : L.Pick($"⚠️ 购买失败：{error.LocalizedDescription}", $"⚠️ The purchase failed: {error.LocalizedDescription}");
        }
    }

    /// <summary>只为拿本地化价格；失败就让它失败，不影响购买流程。</summary>
    private sealed class ProductsDelegate : StoreKit.SKProductsRequestDelegate
    {
        public string? Price { get; private set; }

        public override void ReceivedResponse(StoreKit.SKProductsRequest request,
                                              StoreKit.SKProductsResponse response)
        {
            var p = response.Products?.FirstOrDefault(x => x.ProductIdentifier == EntitlementStore.ProductId);
            if (p is null) return;
            try
            {
                var fmt = new Foundation.NSNumberFormatter
                {
                    NumberStyle = Foundation.NSNumberFormatterStyle.Currency,
                    Locale = p.PriceLocale,
                };
                Price = fmt.StringFromNumber(p.Price);
            }
            catch (Exception ex)
            {
                // 价格只是按钮上的一行字，取不到就不显示 —— 别为一个装饰性字段把购买流程拖垮。
                ErrorLog.Warning("[IAP]", $"价格格式化失败：{ex.Message}");
            }
        }

        public override void RequestFailed(StoreKit.SKRequest request, Foundation.NSError error)
            => ErrorLog.Warning("[IAP]", $"取产品信息失败：{error.LocalizedDescription}");
    }

#elif ANDROID

    private static BillingClient? _client;
    private static TaskCompletionSource<IapResult>? _pending;
    private static string? _price;
    private static bool _ready;

    // 响应码走绑定投影出来的 `BillingResponseCode` 枚举（`Ok` / `UserCancelled` /
    // `ItemAlreadyOwned` / `ServiceDisconnected` …）。
    //
    // ⚠ 枚举成员名是 **`UserCancelled`（双 l）** —— 按 Java 原名的拼法写成 `UserCanceled`
    //   会 CS0117。这类"看着该有、其实拼法不同"的名字第一次编译就会红，别凭记忆写。

    public static void Start()
    {
        if (_client is not null) return;
        try
        {
            var client = BillingClient.NewBuilder(Android.App.Application.Context)
                .SetListener(new PurchasesListener())
                // ⚠ 不调 EnablePendingPurchases 会直接崩（Billing 5 起强制）。
                //   这里只卖一次性商品，所以只开 EnableOneTimeProducts。
                .EnablePendingPurchases(PendingPurchasesParams.NewBuilder().EnableOneTimeProducts().Build())
                .Build();
            _client = client;
            _ = ConnectAsync(client);
        }
        catch (Exception ex)
        {
            ErrorLog.Warning("[IAP]", $"Start 失败：{ex.Message}");
        }
    }

    public static string? PriceText => _price;

    /// <summary>
    /// 连上 Play 之后**顺手查一次已购** —— 这一步就是 Android 版的"恢复购买"：
    /// Play 没有 iOS 那种单独的恢复动作，**以服务端记录为准**是这里唯一的真相来源。
    ///
    /// ⚠ 与 iOS 一样，"查得到但没有我们这个商品" ⇒ 资格是没有的（写 false）。
    /// 但**查询本身失败**（断网、Play 服务没起来）一个字都不能动。
    /// </summary>
    private static async Task ConnectAsync(BillingClient client)
    {
        try
        {
            var r = await client.StartConnectionAsync();
            if (r.ResponseCode != BillingResponseCode.Ok)
            {
                ErrorLog.Warning("[IAP]", $"连接 Play 失败：code={r.ResponseCode} {r.DebugMessage}");
                return;
            }
            _ready = true;
            await RefreshAsync(client);
            await FetchPriceAsync(client);
        }
        catch (Exception ex)
        {
            ErrorLog.Warning("[IAP]", $"连接 Play 异常：{ex.Message}");
        }
    }

    /// <summary>查已购并据此刷新资格。**这是 Android 侧唯一的资格真值来源。**</summary>
    private static async Task<bool> RefreshAsync(BillingClient client)
    {
        try
        {
            var q = QueryPurchasesParams.NewBuilder()
                .SetProductType(BillingClient.ProductType.Inapp)
                .Build();
            var res = await client.QueryPurchasesAsync(q);
            if (res.Result?.ResponseCode != BillingResponseCode.Ok) return false;

            bool owned = false;
            foreach (var p in res.Purchases ?? [])
            {
                if (p.PurchaseState != PurchaseState.Purchased) continue;   // Pending 不算
                if (p.Products is null || !p.Products.Contains(EntitlementStore.ProductId)) continue;
                owned = true;
                await AcknowledgeIfNeededAsync(client, p);
            }
            EntitlementStore.Set(owned);
            return true;
        }
        catch (Exception ex)
        {
            ErrorLog.Warning("[IAP]", $"查询已购失败：{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// ⚠ <b>买完必须应答（acknowledge），否则 Google 三天后自动退款</b> ——
    /// 这是 Play 与 App Store 最大的语义差别，漏掉它的表现是"用户买了、三天后钱退回去、功能也没了"。
    /// </summary>
    private static async Task AcknowledgeIfNeededAsync(BillingClient client, Purchase p)
    {
        if (p.IsAcknowledged) return;
        try
        {
            var ack = AcknowledgePurchaseParams.NewBuilder().SetPurchaseToken(p.PurchaseToken).Build();
            var r = await client.AcknowledgePurchaseAsync(ack);
            if (r.ResponseCode != BillingResponseCode.Ok)
                ErrorLog.Warning("[IAP]", $"应答购买失败：code={r.ResponseCode} {r.DebugMessage}");
        }
        catch (Exception ex)
        {
            ErrorLog.Warning("[IAP]", $"应答购买异常：{ex.Message}");
        }
    }

    /// <summary>取本地化价格（失败就算了，按钮上少一行字而已）。</summary>
    private static async Task FetchPriceAsync(BillingClient client)
    {
        try
        {
            var pd = await QueryProductAsync(client);
            _price = pd?.GetOneTimePurchaseOfferDetails()?.FormattedPrice;
        }
        catch (Exception ex)
        {
            ErrorLog.Warning("[IAP]", $"取价格失败：{ex.Message}");
        }
    }

    private static async Task<ProductDetails?> QueryProductAsync(BillingClient client)
    {
        var spec = QueryProductDetailsParams.Product.NewBuilder()
            .SetProductId(EntitlementStore.ProductId)
            .SetProductType(BillingClient.ProductType.Inapp)
            .Build();
        var p = QueryProductDetailsParams.NewBuilder()
            .SetProductList(new List<QueryProductDetailsParams.Product> { spec })
            .Build();
        var res = await client.QueryProductDetailsAsync(p);
        return res.ProductDetailsList?.FirstOrDefault(d => d.ProductId == EntitlementStore.ProductId);
    }

    /// <summary>
    /// 发起购买。与 iOS 同一条纪律：**先建 TCS 再下单**。
    ///
    /// ⚠ <c>LaunchBillingFlow</c> 必须拿到一个**前台 Activity**，所以这里取
    /// <c>Platform.CurrentActivity</c> —— 它是 null 说明 App 不在前台，此时买不了
    /// （给一句人话，别抛异常）。
    /// </summary>
    public static async Task<IapResult> PurchaseAsync(string productId)
    {
        if (_pending is not null)
            return new IapResult(false, L.Pick("⏳ 上一次操作还在进行中，请稍候。", "⏳ The previous purchase is still in progress."));

        var client = _client;
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (client is null || activity is null)
            return new IapResult(false, L.Pick("⚠️ 现在无法购买（Play 商店未就绪）。请稍后重试。",
                                              "⚠️ Cannot purchase right now (Google Play is not ready). Please try again shortly."));
        if (!_ready)
            return new IapResult(false, L.Pick("⚠️ 还没连上 Play 商店，请稍后重试。",
                                              "⚠️ Not connected to Google Play yet. Please try again shortly."));

        try
        {
            var pd = await QueryProductAsync(client);
            if (pd is null)
                return new IapResult(false, L.Pick("⚠️ 商店里查不到这个商品（多半是后台还没配置好，或所在地区不可用）。",
                                                  "⚠️ This product was not found in the store (usually it is not configured yet, or unavailable in this region)."));

            _pending = new TaskCompletionSource<IapResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var flow = BillingFlowParams.NewBuilder()
                .SetProductDetailsParamsList(new List<BillingFlowParams.ProductDetailsParams>
                {
                    BillingFlowParams.ProductDetailsParams.NewBuilder().SetProductDetails(pd).Build(),
                })
                .Build();
            var r = client.LaunchBillingFlow(activity, flow);
            if (r.ResponseCode != BillingResponseCode.Ok)
                return End(false, L.Pick($"⚠️ 无法打开支付界面（code={r.ResponseCode}）。",
                                         $"⚠️ Could not open the payment sheet (code={r.ResponseCode})."));
            return await _pending.Task;
        }
        catch (Exception ex)
        {
            return End(false, L.Pick($"⚠️ 购买失败：{ex.Message}", $"⚠️ The purchase failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// 「恢复购买」。Android 上**没有独立的恢复动作** —— 它就是"重新问一遍 Play 我买过什么"，
    /// 与启动时那次查询是同一段代码（<see cref="RefreshAsync"/>）。
    /// </summary>
    public static async Task<IapResult> RestoreAsync()
    {
        var client = _client;
        if (client is null)
            return new IapResult(false, L.Pick("⚠️ 现在无法恢复购买（Play 商店未就绪）。",
                                              "⚠️ Cannot restore right now (Google Play is not ready)."));
        if (!_ready)
            await ConnectAsync(client);

        if (!await RefreshAsync(client))
            // ⚠ 查询失败**不动资格**（与 iOS 那条同一个理由：失败的是这次查询，不是那笔购买）
            return new IapResult(false, L.Pick("⚠️ 恢复购买失败（多半是网络问题），请稍后重试。",
                                              "⚠️ Restore failed (usually a network problem). Please try again shortly."));

        return new IapResult(true, EntitlementStore.IsFull
            ? L.Pick("✅ 已恢复购买。", "✅ Purchase restored.")
            : L.Pick("没有找到可恢复的购买记录。", "No previous purchase was found to restore."));
    }

    private static IapResult End(bool ok, string message)
    {
        var tcs = _pending;
        _pending = null;
        tcs?.TrySetResult(new IapResult(ok, message));
        return new IapResult(ok, message);
    }

    /// <summary>
    /// 购买结果回调 —— <c>LaunchBillingFlow</c> 返回的只是"支付界面开没开起来"，
    /// **用户到底付没付从这条回调来**。
    /// </summary>
    private sealed class PurchasesListener : Java.Lang.Object, IPurchasesUpdatedListener
    {
        public void OnPurchasesUpdated(BillingResult result, IList<Purchase>? purchases)
        {
            TryRun(async () =>
            {
                if (result.ResponseCode != BillingResponseCode.Ok)
                {
                    var msg = result.ResponseCode switch
                    {
                        BillingResponseCode.UserCancelled => L.Pick("已取消购买。", "Purchase cancelled."),
                        BillingResponseCode.ItemAlreadyOwned => L.Pick("这个 Apple/Google 账号已经买过了，正在恢复……",
                                                          "This account already owns it - restoring…"),
                        BillingResponseCode.ServiceDisconnected => L.Pick("⚠️ 与 Play 商店的连接断了，请重试。",
                                                            "⚠️ The connection to Google Play dropped. Please try again."),
                        _ => L.Pick($"⚠️ 购买失败（code={result.ResponseCode}）。",
                                    $"⚠️ The purchase failed (code={result.ResponseCode})."),
                    };
                    // 「已经买过了」不是失败：资格是有的，按已购处理
                    if (result.ResponseCode == BillingResponseCode.ItemAlreadyOwned && _client is not null)
                        await RefreshAsync(_client);
                    End(result.ResponseCode == BillingResponseCode.ItemAlreadyOwned, msg);
                    return;
                }

                if (_client is not null)
                    foreach (var p in purchases ?? [])
                        await AcknowledgeIfNeededAsync(_client, p);

                bool ok = _client is not null && await RefreshAsync(_client);
                End(ok, EntitlementStore.IsFull
                    ? L.Pick("✅ 已解锁全能版，谢谢支持！", "✅ The Full Edition is unlocked - thank you!")
                    : L.Pick("⚠️ 交易已提交，但还没确认到账，请稍后在设置里点「恢复购买」。",
                             "⚠️ The transaction was submitted but not confirmed yet. Please tap Restore later in Settings."));
            });
        }

        /// <summary>
        /// 回调是 **Java 线程**上来的，抛出去没有任何人接得住（进程级未处理异常）。
        /// 这里必须自己兜住 —— 与 `MauiVml`/`ShellPage` 那两处 `async void` 的教训同源。
        /// </summary>
        private static void TryRun(Func<Task> body)
        {
            _ = Task.Run(async () =>
            {
                try { await body(); }
                catch (Exception ex) { ErrorLog.Warning("[IAP]", $"购买回调异常：{ex}"); }
            });
        }
    }

#else

    public static void Start() { }
    public static string? PriceText => null;
    public static Task<IapResult> PurchaseAsync(string productId) => Task.FromResult(NotSupported());
    public static Task<IapResult> RestoreAsync() => Task.FromResult(NotSupported());

    private static IapResult NotSupported()
        => new(false, L.Pick("本平台没有内购。", "In-app purchase is not available on this platform."));

#endif
}
