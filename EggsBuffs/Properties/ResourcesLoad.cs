using UnityEngine;
using R2API;

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
        }
    }
}
