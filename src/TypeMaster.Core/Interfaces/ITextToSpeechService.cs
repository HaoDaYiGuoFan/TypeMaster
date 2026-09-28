namespace TypeMaster.Core.Interfaces;

/// <summary>
/// 语音朗读服务：把学习园地的文字读出来（五笔字根口诀、拼音声韵母等）。
///
/// 设计要点：
/// 1) 实现基于 Windows 内置语音（SAPI），不联网、不上传任何内容；
/// 2) 朗读开关与语速由 <see cref="TypeMaster.Core.AppState"/> 的配置决定，
///    调用方无需自行判断开关，直接调用 <see cref="Speak"/> 即可；
/// 3) 系统缺少中文语音时 <see cref="IsAvailable"/> 为 false，
///    调用方据此禁用按钮并展示 <see cref="UnavailableHint"/>。
/// </summary>
public interface ITextToSpeechService
{
    /// <summary>系统是否存在可用的中文语音。为 false 时朗读不可用（不应崩溃）。</summary>
    bool IsAvailable { get; }

    /// <summary>朗读不可用时的提示文案（可直接展示给用户）。</summary>
    string UnavailableHint { get; }

    /// <summary>
    /// 朗读一段文本。会在朗读前打断上一段（避免连点排队）。
    /// </summary>
    /// <param name="text">要朗读的文本</param>
    /// <returns>真正开始朗读返回 true；被开关关闭、无语音或文本为空返回 false</returns>
    bool Speak(string text);

    /// <summary>立即停止当前朗读。</summary>
    void Stop();
}
