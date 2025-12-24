using System.Collections.Generic;
using UnityEngine;

public static class ItemIconLoader
{
    private static readonly Dictionary<string, Sprite> Cache = new();

    public static Sprite Load(string iconKey)
    {
        if (string.IsNullOrEmpty(iconKey)) return null;

        // 확장자 방어
        iconKey = iconKey.Replace(".png", "").Replace(".jpg", "").Replace(".jpeg", "");

        if (Cache.TryGetValue(iconKey, out var cached))
            return cached;

        Sprite sp = null;

        // 1) Sprite 직로드: ItemIcons/iconKey
        sp = Resources.Load<Sprite>($"ItemIcons/{iconKey}");

        // 2) 폴더 구조: ItemIcons/iconKey/iconKey
        if (sp == null)
            sp = Resources.Load<Sprite>($"ItemIcons/{iconKey}/{iconKey}");

        // 3) 하위 전체에서 이름 매칭(스프라이트 시트/서브에셋 대응)
        if (sp == null)
        {
            var all = Resources.LoadAll<Sprite>("ItemIcons");
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == iconKey)
                {
                    sp = all[i];
                    break;
                }
            }
        }

        // 4) Sprite가 아니라 Texture2D로 들어온 경우까지 커버 (가장 강력)
        if (sp == null)
        {
            Texture2D tex = Resources.Load<Texture2D>($"ItemIcons/{iconKey}");
            if (tex == null)
                tex = Resources.Load<Texture2D>($"ItemIcons/{iconKey}/{iconKey}");

            if (tex != null)
            {
                sp = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                sp.name = iconKey; // 디버그 편하게
            }
        }

        Cache[iconKey] = sp; // null도 캐시
        return sp;
    }

    public static void ClearCache() => Cache.Clear();
}
