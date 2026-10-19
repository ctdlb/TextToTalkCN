using System.Collections.Generic;

namespace TextToTalk.Localization;

/// <summary>
/// 把语音服务返回的英文语音 ID 转成中文显示名。
/// 只影响界面显示，保存的仍是原始 ID（如 en-US-JennyNeural）。
/// </summary>
public static class VoiceDisplayNames
{
    private static readonly Dictionary<string, string> Locales = new()
    {
        ["en-US"] = "英语（美国）",
        ["en-GB"] = "英语（英国）",
        ["en-AU"] = "英语（澳大利亚）",
        ["en-CA"] = "英语（加拿大）",
        ["en-IN"] = "英语（印度）",
        ["en-IE"] = "英语（爱尔兰）",
        ["en-NZ"] = "英语（新西兰）",
        ["zh-CN"] = "中文（中国大陆）",
        ["zh-TW"] = "中文（台湾）",
        ["zh-HK"] = "中文（香港）",
        ["ja-JP"] = "日语（日本）",
        ["ko-KR"] = "韩语（韩国）",
        ["de-DE"] = "德语（德国）",
        ["fr-FR"] = "法语（法国）",
        ["fr-CA"] = "法语（加拿大）",
        ["es-ES"] = "西班牙语（西班牙）",
        ["es-MX"] = "西班牙语（墨西哥）",
        ["it-IT"] = "意大利语（意大利）",
        ["pt-BR"] = "葡萄牙语（巴西）",
        ["pt-PT"] = "葡萄牙语（葡萄牙）",
        ["ru-RU"] = "俄语（俄罗斯）",
        ["pl-PL"] = "波兰语（波兰）",
        ["nl-NL"] = "荷兰语（荷兰）",
        ["tr-TR"] = "土耳其语（土耳其）",
        ["ar-SA"] = "阿拉伯语（沙特）",
        ["ar-EG"] = "阿拉伯语（埃及）",
        ["hi-IN"] = "印地语（印度）",
        ["th-TH"] = "泰语（泰国）",
        ["vi-VN"] = "越南语（越南）",
        ["id-ID"] = "印尼语（印尼）",
        ["cs-CZ"] = "捷克语（捷克）",
        ["da-DK"] = "丹麦语（丹麦）",
        ["fi-FI"] = "芬兰语（芬兰）",
        ["nb-NO"] = "挪威语（挪威）",
        ["sv-SE"] = "瑞典语（瑞典）",
        ["hu-HU"] = "匈牙利语（匈牙利）",
        ["ro-RO"] = "罗马尼亚语（罗马尼亚）",
        ["el-GR"] = "希腊语（希腊）",
        ["he-IL"] = "希伯来语（以色列）",
        ["uk-UA"] = "乌克兰语（乌克兰）",
        ["wuu-CN"] = "中文（吴语）",
        ["yue-CN"] = "中文（粤语）",
    };

    private static readonly Dictionary<string, string> Dialects = new()
    {
        ["liaoning"] = "中文（辽宁）",
        ["shaanxi"] = "中文（陕西）",
        ["henan"] = "中文（河南）",
        ["shandong"] = "中文（山东）",
        ["sichuan"] = "中文（四川）",
        ["wu"] = "中文（吴语）",
        ["anhui"] = "中文（安徽）",
    };

    private static readonly Dictionary<string, string> Names = new()
    {
        // 中文
        ["Xiaoxiao"] = "晓晓",
        ["Xiaoyi"] = "晓伊",
        ["Xiaochen"] = "晓辰",
        ["Xiaohan"] = "晓涵",
        ["Xiaomeng"] = "晓梦",
        ["Xiaomo"] = "晓墨",
        ["Xiaorui"] = "晓睿",
        ["Xiaoshuang"] = "晓双",
        ["Xiaoyan"] = "晓颜",
        ["Xiaoyou"] = "晓悠",
        ["Xiaozhen"] = "晓甄",
        ["Xiaobei"] = "晓北",
        ["Xiaoni"] = "晓妮",
        ["Xiaoxuan"] = "晓萱",
        ["Yunxi"] = "云希",
        ["Yunjian"] = "云健",
        ["Yunyang"] = "云扬",
        ["Yunfeng"] = "云枫",
        ["Yunhao"] = "云皓",
        ["Yunjie"] = "云杰",
        ["Yunxia"] = "云夏",
        ["Yunye"] = "云野",
        ["Yunze"] = "云泽",
        ["HsiaoChen"] = "晓臻",
        ["HsiaoYu"] = "晓雨",
        ["YunJhe"] = "云哲",
        ["HiuGaai"] = "晓佳",
        ["HiuMaan"] = "晓曼",
        ["WanLung"] = "云龙",
        ["Yundeng"] = "云登",
        ["Yunxiang"] = "云翔",
        ["Yunyi"] = "云逸",
        ["Xiaoke"] = "晓可",
        ["Xiaoqi"] = "晓琪",
        ["Xiaorou"] = "晓柔",
        ["Xiaoyu"] = "晓雨",
        ["Xiaotong"] = "晓彤",
        ["Xiaoxiao2"] = "晓晓 2",
        ["XiaoxiaoDialects"] = "晓晓（方言）",
        ["Xiaoqiu"] = "晓秋",
        ["XiaoMin"] = "晓敏",
        ["Bo"] = "博",
        ["Grant"] = "格兰特",
        ["Harper"] = "哈珀",
        ["Lan"] = "兰",
        ["Mei"] = "梅",
        ["Wei"] = "伟",
        ["Yunzhe"] = "云哲",
        ["YunSong"] = "云松",
        ["Yunxiao"] = "云霄",
        ["Yunbiao"] = "云彪",
        ["Yunfan"] = "云帆",
        ["Yunqi"] = "云起",
        ["Yunhan"] = "云涵",

        // 英语（美国）
        ["Jenny"] = "珍妮",
        ["Guy"] = "盖伊",
        ["Aria"] = "阿丽亚",
        ["Davis"] = "戴维斯",
        ["Jane"] = "简",
        ["Jason"] = "杰森",
        ["Kai"] = "凯",
        ["Luna"] = "露娜",
        ["Sara"] = "莎拉",
        ["Tony"] = "托尼",
        ["Nancy"] = "南希",
        ["Cora"] = "科拉",
        ["Christopher"] = "克里斯托弗",
        ["Brandon"] = "布兰登",
        ["Amber"] = "安珀",
        ["Ana"] = "安娜",
        ["Ashley"] = "阿什莉",
        ["Andrew"] = "安德鲁",
        ["Emma"] = "艾玛",
        ["Brian"] = "布莱恩",
        ["Ava"] = "艾娃",
        ["Adam"] = "亚当",
        ["Jacob"] = "雅各布",
        ["Michelle"] = "米歇尔",
        ["Monica"] = "莫妮卡",
        ["Roger"] = "罗杰",
        ["Steffan"] = "斯特凡",
        ["Eric"] = "埃里克",
        ["AIGenerate1"] = "AI 生成 1",
        ["AIGenerate2"] = "AI 生成 2",

        // 英语（英国）
        ["Libby"] = "莉比",
        ["Maisie"] = "梅茜",
        ["Ryan"] = "瑞安",
        ["Sonia"] = "索尼娅",
        ["Thomas"] = "托马斯",
        ["Alfie"] = "阿尔菲",
        ["Abbi"] = "阿比",
        ["Bella"] = "贝拉",
        ["Elliot"] = "埃利奥特",
        ["Ethan"] = "伊桑",
        ["Hollie"] = "霍莉",
        ["Noah"] = "诺亚",
        ["Oliver"] = "奥利弗",
        ["Olivia"] = "奥利维亚",

        // 日语
        ["Nanami"] = "七海",
        ["Keita"] = "圭太",
        ["Mayu"] = "真由",
        ["Daichi"] = "大地",
        ["Naoki"] = "直树",
        ["Shiori"] = "诗织",
        ["Masako"] = "雅子",
        ["Ayumi"] = "步美",
        ["Ichiro"] = "一郎",
    };

    private static readonly Dictionary<string, string> AzureStyleNames = new()
    {
        ["general"] = "通用",
        ["chat"] = "聊天",
        ["customerservice"] = "客服",
        ["assistant"] = "助手",
        ["newscast"] = "新闻播报",
        ["narration-professional"] = "旁白（专业）",
        ["narration-relaxed"] = "旁白（轻松）",
        ["documentary-narration"] = "纪录片旁白",
        ["sports-commentary"] = "体育解说",
        ["live-commercial"] = "直播带货",
        ["poetry-reading"] = "诗歌朗诵",
        ["affectionate"] = "亲切",
        ["angry"] = "生气",
        ["calm"] = "平静",
        ["cheerful"] = "愉悦",
        ["sad"] = "悲伤",
        ["excited"] = "兴奋",
        ["friendly"] = "友好",
        ["hopeful"] = "期盼",
        ["shouting"] = "喊叫",
        ["terrified"] = "恐惧",
        ["unfriendly"] = "冷淡",
        ["whispering"] = "耳语",
        ["sorry"] = "歉意",
        ["empathetic"] = "共情",
        ["debating"] = "辩论",
        ["lyrical"] = "抒情",
        ["serious"] = "严肃",
        ["gentle"] = "温柔",
        ["supportive"] = "鼓励",
        ["disgruntled"] = "不满",
        ["fearful"] = "害怕",
        ["embarrassed"] = "尴尬",
        ["anxious"] = "焦虑",
        ["curious"] = "好奇",
        ["disappointed"] = "失望",
        ["encouraging"] = "鼓励",
        ["guilty"] = "内疚",
        ["lonely"] = "孤独",
        ["surprised"] = "惊讶",
        ["sentimental"] = "感伤",
        ["story-telling"] = "讲故事",
        ["tired"] = "疲惫",
    };

    /// <summary>
    /// 把 Azure 语音风格（如 cheerful）转成中文显示名；未知风格原样返回。
    /// 只用于显示，实际传给 Azure 的仍是英文风格 ID。
    /// </summary>
    public static string AzureStyle(string? style)
    {
        if (string.IsNullOrWhiteSpace(style)) return string.Empty;
        return AzureStyleNames.TryGetValue(style!, out var localized) ? localized : style!;
    }

    private static string StyleSuffix(string style)
    {
        if (style.Contains("HD") || style.Contains("Dragon")) return "（高清）";

        var localized = style
            .Replace("MAI-Voice-", "MAI ")
            .Replace("-Flash", " 快速版")
            .Replace("MAI-Voice", "MAI");

        return $"（{localized}）";
    }

    /// <summary>
    /// 把 Azure 语音 ID（如 en-US-JennyNeural、zh-CN-Yunyi:DragonHDFlashLatest、
    /// zh-CN-liaoning-XiaobeiNeural）转成中文显示名。
    /// 未知语音回退为「原始名称 · 语言」。
    /// </summary>
    public static string Azure(string? shortName)
    {
        if (string.IsNullOrWhiteSpace(shortName)) return string.Empty;

        var parts = shortName!.Split('-', 3);
        if (parts.Length < 3) return shortName;

        var locale = $"{parts[0]}-{parts[1]}";
        var raw = parts[2];

        // 新版 HD 语音用冒号带风格，如 Yunyi:DragonHDFlashLatest
        string? style = null;
        var colon = raw.IndexOf(':');
        if (colon >= 0)
        {
            style = raw[(colon + 1)..];
            raw = raw[..colon];
        }

        var multilingual = raw.Contains("Multilingual");
        raw = raw.Replace("MultilingualNeural", string.Empty)
            .Replace("Multilingual", string.Empty)
            .Replace("Neural", string.Empty);

        // 方言语音形如 liaoning-Xiaobei，最后一段才是名字
        var segments = raw.Split('-');
        var dialect = segments.Length > 1 ? segments[0] : null;
        var name = segments[^1];

        var displayName = Names.TryGetValue(name, out var localized) ? localized : name;

        string localeName;
        if (dialect != null && Dialects.TryGetValue(dialect, out var localizedDialect))
        {
            localeName = localizedDialect;
        }
        else
        {
            localeName = Locales.TryGetValue(locale, out var localizedLocale) ? localizedLocale : locale;
        }

        string suffix;
        if (multilingual)
        {
            suffix = "（多语言）";
        }
        else if (style != null)
        {
            suffix = StyleSuffix(style);
        }
        else
        {
            suffix = string.Empty;
        }

        return string.IsNullOrEmpty(localeName)
            ? $"{displayName}{suffix}"
            : $"{displayName}{suffix} · {localeName}";
    }
}
