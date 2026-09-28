namespace TypeMaster.Core.Interfaces;

/// <summary>
/// 音效服务：提供打字练习与小游戏所需的全部提示音。
/// </summary>
public interface ISoundService
{
    /// <summary>普通按键音</summary>
    void PlayKey();

    /// <summary>错误警告音</summary>
    void PlayError();

    /// <summary>完成提示音</summary>
    void PlayComplete();

    #region 小游戏音效

    /// <summary>锁定目标（发射 / 起手）音</summary>
    void PlayShoot();

    /// <summary>消灭目标得分音</summary>
    void PlayHit();

    /// <summary>漏掉目标、损失生命音</summary>
    void PlayMiss();

    /// <summary>连击里程碑音，音调随连击数升高</summary>
    /// <param name="combo">当前连击数</param>
    void PlayCombo(int combo);

    /// <summary>开局倒数提示音</summary>
    void PlayStart();

    /// <summary>游戏结束音</summary>
    void PlayGameOver();

    /// <summary>地鼠冒头音</summary>
    void PlayPop();

    /// <summary>青蛙吃虫的咀嚼音</summary>
    void PlayEat();

    /// <summary>抓到小偷音</summary>
    void PlayCatch();

    #endregion 小游戏音效
}
