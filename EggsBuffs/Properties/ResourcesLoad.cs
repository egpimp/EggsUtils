using RoR2;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EggsUtils.Properties
{
    public static class EggAssets
    {
        //Paths to buffdefs we steal icons from
        internal static string trackingDefPath = "RoR2/Base/CritOnUse/bdFullCrit.asset";
        internal static string armorDefPath = "RoR2/Base/Common/bdArmorBoost.asset";
        internal static Sprite doesNotExist;

        //Path for our lang folder
        internal const string LangFolder = "egmods_languages";
        internal static string RootLangFolderPath => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), LangFolder);

        internal static void RegisterAssets()
        {
            if (Directory.Exists(RootLangFolderPath)) Language.collectLanguageRootFolders += RegisterTokensFolder;
            else Log.LogError("Could not find eggmods language folder");
            Log.LogMessage("Tokens registered");
            RegisterSprites();
            Log.LogMessage("Buff Icons registered");
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

        private static void RegisterTokensFolder(List<string> list)
        {
            //Add our folder full of language tokens to be loaded
            list.Add(RootLangFolderPath);
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
