using UnityEngine;
using R2API;
using System;
using Mono.Cecil;

namespace EggsUtils.Properties
{
    public static class Assets
    {
        internal static Sprite trackingIcon = UnityEngine.Resources.Load<Sprite>("Textures/BuffIcons/texBuffFullCritIcon");
        internal static Sprite placeHolderIcon = UnityEngine.Resources.Load<Sprite>("Textures/BuffIcons/texBuffPulverizeIcon");
        internal static void RegisterTokens()
        {
            LanguageAPI.Add("KEYWORD_MARKING", "<style=cKeywordName>Tracking</style><style=cSub>Slows enemies and increases damage towards them</style>");
            LanguageAPI.Add("KEYWORD_STASIS", "<style=cKeywordName>Stasis</style><style=cSub>Units in stasis are invulnerable but cannot act</style>");
            LanguageAPI.Add("KEYWORD_ADAPTIVE", "<style=cKeywordName>Adaptive</style><style=cSub>Incoming instances of damage are limited to 20% of max health</style>");
            LanguageAPI.Add("KEYWORD_LUCKY", "<style=cKeywordName>Lucky</style><style=cSub>Rerolls all random effects x times for a favorable outcome</style>");
            LanguageAPI.Add("KEYWORD_PREPARE", "<style=cKeywordName>Prepare</style><style=cSub>Refreshes a stock of an ability</style>");
        }
        public static Sprite TexToSprite(Texture2D tex)
        {
            return Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        public static AssetBundle LoadAssetBundle(Byte[] resourceBytes)
        {
            if (resourceBytes == null) throw new ArgumentNullException(nameof(Resource));
            var bundle = AssetBundle.LoadFromMemory(resourceBytes);
            return bundle;
        }
    }
}
