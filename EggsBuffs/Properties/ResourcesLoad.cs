using UnityEngine;
using R2API;
using System;
using Mono.Cecil;
using RoR2;
using UnityEngine.AddressableAssets;

namespace EggsUtils.Properties
{
    public static class Assets
    {
        //Paths to buffdefs we steal icons from
        internal static string trackingDefPath = "RoR2/Base/CritOnUse/bdFullCrit.asset";
        internal static string armorDefPath = "RoR2/Base/Common/bdArmorBoost.asset";
        internal static Sprite doesNotExist;

        internal static void RegisterAssets()
        {
            RegisterTokens();
            EggsUtils.LogToConsole("Tokens registered");
            RegisterSprites();
            EggsUtils.LogToConsole("Buff Icons registered");
        }

        private static void RegisterSprites()
        {
            //Grab the buffdefs (So scuffed)
            BuffDef trackingDef = Addressables.LoadAssetAsync<BuffDef>(trackingDefPath).WaitForCompletion();
            BuffDef armorDef = Addressables.LoadAssetAsync<BuffDef>(armorDefPath).WaitForCompletion();

            //Steal the icons from them (Bruh)
            Buffs.BuffsLoading.buffDefAdaptive.iconSprite = armorDef.iconSprite;
            Buffs.BuffsLoading.buffDefCunning.iconSprite = trackingDef.iconSprite;
            Buffs.BuffsLoading.buffDefTracking.iconSprite = trackingDef.iconSprite;
        }

        private static void RegisterTokens()
        {
            //Establish all the keyword tokens
            LanguageAPI.Add("KEYWORD_ENHANCING", "<style=cKeywordName>Overloading</style><style=cSub>Consumes all charges on use but becomes stronger for each charge consumed</style>");
            LanguageAPI.Add("KEYWORD_MARKING", "<style=cKeywordName>Tracking</style><style=cSub>Slows enemies and increases damage towards them</style>");
            LanguageAPI.Add("KEYWORD_STASIS", "<style=cKeywordName>Stasis</style><style=cSub>Units in stasis are invulnerable but cannot act</style>");
            LanguageAPI.Add("KEYWORD_ADAPTIVE", "<style=cKeywordName>Unyielding</style><style=cSub>Knockback immunity and incoming instances of damage are limited to 20% of max health</style>");
            LanguageAPI.Add("KEYWORD_PREPARE", "<style=cKeywordName>Prepare</style><style=cSub>Refreshes a stock of an ability</style>");
        }

        //Converts a 2d tex to an actual sprite
        public static Sprite TexToSprite(Texture2D tex)
        {
            //Just returns appropriately designed sprite
            return Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        //Loads a given assetbundle so I can pull what I want off of it
        public static AssetBundle LoadAssetBundle(Byte[] resourceBytes)
        {
            //If the resources are null for some reason throw exception
            if (resourceBytes == null) throw new ArgumentNullException(nameof(Resource));
            //Bundle is just the assetbundle being loaded from memory
            var bundle = AssetBundle.LoadFromMemory(resourceBytes);
            //Then return the bundle
            return bundle;
        }
    }
}
