using UnityEngine;
using R2API;

namespace EggsBuffs.Properties
{
    public static class Assets
    {
        public static Sprite trackingIcon = Resources.Load<Sprite>("Textures/BuffIcons/texBuffFullCritIcon");
        public static Sprite placeHolderIcon = Resources.Load<Sprite>("Textures/BuffIcons/texBuffPulverizeIcon");
        internal static void RegisterTokens()
        {
            LanguageAPI.Add("KEYWORD_MARKING", "<style=cKeywordName>Tracking</style><style=cSub>Slows enemies and increases damage towards them</style>");
            LanguageAPI.Add("KEYWORD_STASIS", "<style=cKeywordName>Stasis</style><style=cSub>Units in stasis are invulnerable but cannot act</style>");
            LanguageAPI.Add("KEYWORD_ADAPTIVE", "<style=cKeywordName>Adaptive Shielding</style><style=cSub>Incoming instances of damage are limited to 20% of max health</style>");
        }
    }
}
