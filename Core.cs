using System.Collections;
using Il2CppScheduleOne.Dialogue;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Il2CppScheduleOne.Quests;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.Interaction;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Il2CppFluffyUnderware.DevTools.Extensions;
using Il2CppInterop.Runtime;
using Il2CppScheduleOne.NPCs.CharacterClasses;
using static Il2CppScheduleOne.NPCs.Relation.NPCRelationData;
using static ProperPropertyProgression.ProperPropertyProgression;
using HarmonyLib;
using UnityEngine.Events;

[assembly: MelonInfo(typeof(ProperPropertyProgression.ProperPropertyProgression), "ProperPropertyProgression", "1.1.3", "Soul", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ProperPropertyProgression
{
    public class ProperPropertyProgression : MelonMod
    {
        [HarmonyPatch(typeof(Il2CppScheduleOne.Property.RV), nameof(Il2CppScheduleOne.Property.RV.ShouldSave))]
        public class RV_ShouldSave_ForceTrue
        {
            static bool Prefix(ref bool __result)
            {
                __result = true; // finding this single method took me 3 hours. i am not good at what i do
                return false;    
            }
        }

        public static class ModConfig
        {
            private static MelonPreferences_Category PropertyChanges;
            public static MelonPreferences_Entry<bool> ChangeHousePrices;
            public static MelonPreferences_Entry<int> MotelPrice;
            public static MelonPreferences_Entry<int> SweatshopPrice;
            public static MelonPreferences_Entry<int> BungalowPrice;
            public static MelonPreferences_Entry<int> BarnPrice;
            public static MelonPreferences_Entry<int> WarehousePrice;
            public static MelonPreferences_Entry<int> ManorPrice;

            private static MelonPreferences_Category RVChanges;
            public static MelonPreferences_Entry<bool> StartRVEmpty;
            public static MelonPreferences_Entry<int> RVPrice;

            private static MelonPreferences_Category BusinessChanges;
            public static MelonPreferences_Entry<bool> ChangeBusinessPrices;
            public static MelonPreferences_Entry<int> LaundromatPrice;
            public static MelonPreferences_Entry<int> PostOfficePrice;
            public static MelonPreferences_Entry<int> CarWashPrice;
            public static MelonPreferences_Entry<int> TacoTicklersPrice;
            public static MelonPreferences_Entry<bool> ChangeBusinessLaunder;
            public static MelonPreferences_Entry<int> LaundromatLaunder;
            public static MelonPreferences_Entry<int> PostOfficeLaunder;
            public static MelonPreferences_Entry<int> CarWashLaunder;
            public static MelonPreferences_Entry<int> TacoTicklersLaunder;


            public static void Setup()
            {
                PropertyChanges = MelonPreferences.CreateCategory("ProperPropertyProgression", "Proper Property Progression Changes", true);
                ChangeHousePrices = PropertyChanges.CreateEntry("ChangeHousePrices", true, "Change house prices.", "Changes the prices of all the properties.");
                MotelPrice = PropertyChanges.CreateEntry("MotelPrice", 2500, "Hotel Price ", "GAME DEFAULT = 75");
                SweatshopPrice = PropertyChanges.CreateEntry("SweatshopPrice", 8000, "Sweatshop Price ", "GAME DEFAULT = 500 (idk why its not 800, i think the price you pay is separate from its ingame price)");
                BungalowPrice = PropertyChanges.CreateEntry("BungalowPrice", 35000, "Bungalow Price", " GAME DEFAULT = 6000");
                BarnPrice = PropertyChanges.CreateEntry("BarnPrice", 80000, "Barn Price", " GAME DEFAULT = 25000");
                WarehousePrice = PropertyChanges.CreateEntry("WarehousePrice", 100000, "Docks Warehouse Price", " GAME DEFAULT = 50000");
                ManorPrice = PropertyChanges.CreateEntry("ManorPrice", 150000, "Manor Price", " GAME DEFAULT = 50000");

                RVChanges = MelonPreferences.CreateCategory("ProperPropertyProgression", "RVChanges", true);
                StartRVEmpty = RVChanges.CreateEntry("StartRVEmpty", true, "Start RV Empty", "Deletes everything in the RV at the start. (Note, after the RV is destroyed everything is still deleted before you repair it. This just ensure you can't cheese by picking everything up before.)");
                RVPrice = RVChanges.CreateEntry("RVPrice", 500, "RV Price");

                BusinessChanges = MelonPreferences.CreateCategory("ProperPropertyProgression", "Business Changes", true);
                ChangeBusinessPrices = BusinessChanges.CreateEntry("ChangeBusinessPrices", true, "Change house prices.", "Changes the prices of all the businesses.");
                LaundromatPrice = BusinessChanges.CreateEntry("LaundromatPrice", 10000, "Laundromat Price ", "GAME DEFAULT = 4000");
                PostOfficePrice = BusinessChanges.CreateEntry("PostOfficePrice", 20000, "Post Office Price ", "GAME DEFAULT = 10000");
                CarWashPrice = BusinessChanges.CreateEntry("CarWashPrice", 40000, "Car Wash Price ", "GAME DEFAULT = 20000");
                TacoTicklersPrice = BusinessChanges.CreateEntry("TacoTicklersPrice", 70000, "TacoTicklers Price ", "GAME DEFAULT = 50000");
                ChangeBusinessLaunder = BusinessChanges.CreateEntry("ChangeBusinessLaunder", true, "Change house prices.", "Changes the launderamount of all the businesses.");
                LaundromatLaunder = BusinessChanges.CreateEntry("LaundromatLaunder", 5000, "Laundromat Launder Capacity ", "GAME DEFAULT = 2000");
                PostOfficeLaunder = BusinessChanges.CreateEntry("PostOfficeLaunder", 5000, "Post Office Launder Capacity ", "GAME DEFAULT = 4000");
                CarWashLaunder = BusinessChanges.CreateEntry("CarWashLaunder", 5000, "Car Wash Launder Capacity", "GAME DEFAULT = 6000");
                TacoTicklersLaunder = BusinessChanges.CreateEntry("TacoTicklersLaunder", 5000, "TacoTicklers Launder Capacity", " GAME DEFAULT = 8000");

                PropertyChanges.SaveToFile(false);
                RVChanges.SaveToFile(false);
                BusinessChanges.SaveToFile(false);





            }
        }

        public override void OnInitializeMelon()
        {
            ModConfig.Setup();
            LoggerInstance.Msg("Config initialized.");
            Melon<ProperPropertyProgression>.Logger.Msg("Initialized!");
            SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>)OnSceneLoaded);
        }

        public static void UnlockAlbertNow()
        {

            foreach (var albert in UnityEngine.Object.FindObjectsOfType<Albert>())
            {
                if (albert?.gameObject?.name == "Albert")
                {
                  
                   albert.SupplierUnlocked(EUnlockType.Recommendation, true);
                  return;
                }
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name.Equals("Main"))
                MelonCoroutines.Start(Load());
        }

        private static IEnumerator Load()
        {
            for (int i = 0; i < 40; i++)
                yield return new WaitForEndOfFrame();

            Melon<ProperPropertyProgression>.Logger.Msg("Finally! *I* can start!");
            if (ModConfig.ChangeHousePrices.Value)
            {
                DialogueModifier.ApplyPatch();
                DialogueModifier.SetMappedPropertyPrices();
            }
            DialogueModifier.SetBusinessPricesAndCapacities();
            DisableQuestExplosions();
            DialogueModifier.SetQuestActive("Talk to the manager in the motel office");
            DialogueModifier.SetQuestActive("Talk to Mrs. Ming at the Chinese restaurant");
            if (CheckFirstQuest() && ModConfig.StartRVEmpty.Value) ClearRVItemContainers();
            DialogueModifier.SetupMarcoRentRoomChoice();
            yield break;
        }

        public static class DialogueModifier
        {
            private static Dictionary<string, int> PriceMap = new()
            {
                { "Donna", ModConfig.MotelPrice.Value },
                { "Ming", ModConfig.SweatshopPrice.Value }
            };
            private static bool completedRV = false;

            public static void ApplyPatch()
            {
                var allControllers = GameObject.FindObjectsOfType<DialogueController_Ming>();
                foreach (var controller in allControllers)
                {
                    var parentName = controller.gameObject.transform.parent?.name;
                    if (parentName != null && PriceMap.TryGetValue(parentName, out int newPrice))
                    {
                        controller.Price = newPrice;
                    }
                }
            }

            public static void SetQuestActive(string questTitle)
            {
                foreach (var entry in UnityEngine.Object.FindObjectsOfType<QuestEntry>())
                {
                    if (entry.Title == questTitle)
                    {
                        entry.state = EQuestState.Active;
                        return;
                    }
                }
            }

            public static void SetQuestInactive(string questTitle)
            {
                foreach (var entry in UnityEngine.Object.FindObjectsOfType<QuestEntry>())
                {
                    if (entry.Title == questTitle)
                    {
                        entry.state = EQuestState.Inactive;
                        return;
                    }
                }
            }

// Needed for Il2CppType.Of<T>()

public static void SetMappedPropertyPrices()
    {
        var propertyMap = new Dictionary<string, (Il2CppSystem.Type il2cppType, int price)>
    {
        { "Bungalow", (Il2CppType.Of<Bungalow>(), ModConfig.BungalowPrice.Value) },
        { "MotelRoom", (Il2CppType.Of<MotelRoom>(), ModConfig.MotelPrice.Value) },
        { "Sweatshop", (Il2CppType.Of<Sweatshop>(), ModConfig.SweatshopPrice.Value) },
        { "Barn", (Il2CppType.Of<Property>(), ModConfig.BarnPrice.Value) },
        { "DocksWarehouse", (Il2CppType.Of<Property>(), ModConfig.WarehousePrice.Value) },
        { "Manor", (Il2CppType.Of<Property>(), ModConfig.ManorPrice.Value) }
    };

        foreach (var kvp in propertyMap)
        {
            string objectName = kvp.Key;
            Il2CppSystem.Type il2cppType = kvp.Value.il2cppType;
            int price = kvp.Value.price;

            foreach (var comp in UnityEngine.Object.FindObjectsOfType(il2cppType))
            {
                var component = comp.Cast<Il2CppSystem.Object>();
                var unityComponent = component.TryCast<UnityEngine.Component>();

                if (unityComponent != null && unityComponent.gameObject.name == objectName)
                {
                    var priceField = il2cppType.GetField("Price");
                    if (priceField != null)
                    {
                        priceField.SetValue(component, price);
                    }
                }
            }
        }

        Melon<ProperPropertyProgression>.Logger.Msg("Property prices updated from config.");
    }


            public static void SetBusinessPricesAndCapacities()
            {
                var businessMap = new Dictionary<string, (int Price, int LaunderCapacity)>
    {
        { "Laundromat", (ModConfig.LaundromatPrice.Value, ModConfig.LaundromatLaunder.Value) },
        { "PostOffice", (ModConfig.PostOfficePrice.Value, ModConfig.PostOfficeLaunder.Value) },
        { "CarWash", (ModConfig.CarWashPrice.Value, ModConfig.CarWashLaunder.Value) },
        { "TacoTicklers", (ModConfig.TacoTicklersPrice.Value, ModConfig.TacoTicklersLaunder.Value) }
    };

                foreach (var business in UnityEngine.Object.FindObjectsOfType<Business>())
                {
                    string objName = business.gameObject.name.Replace(" ", "").Replace("(Clone)", "").Trim();

                    foreach (var kvp in businessMap)
                    {
                        string expected = kvp.Key.ToLowerInvariant();
                        string actual = objName.ToLowerInvariant();

                        if (actual.Contains(expected))
                        {
                            if (ModConfig.ChangeBusinessPrices.Value)
                            {
                                business.Price = kvp.Value.Price;
                                Melon<ProperPropertyProgression>.Logger.Msg($"Set {business.gameObject.name} to Price {kvp.Value.Price}");
                            }
                            if (ModConfig.ChangeBusinessLaunder.Value)
                            {
                                business.LaunderCapacity = kvp.Value.LaunderCapacity;
                                Melon<ProperPropertyProgression>.Logger.Msg($"Set {business.gameObject.name} to Launder Amount: {kvp.Value.LaunderCapacity}");
                            }

                            break;
                        }
                    }
                }

                Melon<ProperPropertyProgression>.Logger.Msg("Business prices and launder capacities set from dictionary.");
            }





            public static void SetupMarcoRentRoomChoice()
            {
                const string questTitle = "Investigate the explosion";

                GameObject donnaDialogue = null, marcoDialogue = null;
                foreach (var obj in UnityEngine.Object.FindObjectsOfType<GameObject>())
                {
                    if (obj.name == "Dialogue" && obj.transform.parent != null)
                    {
                        var parentName = obj.transform.parent.name;
                        if (parentName == "Donna") donnaDialogue = obj;
                        else if (parentName == "Marco") marcoDialogue = obj;
                    }
                }

                var marcoController = marcoDialogue.GetComponent<DialogueController>();
                var donnaController = donnaDialogue.GetComponent<DialogueController_Ming>();
                var sourceChoice = donnaController.Choices[3];

                var clonedConversation = UnityEngine.Object.Instantiate(sourceChoice.Conversation);
                clonedConversation.name = "Marco_RentRoom_Conversation";

                var newChoice = new DialogueController.DialogueChoice
                {
                    ChoiceText = $"Hey, could you fix my RV? It uh, had an accident...  [${ModConfig.RVPrice.Value} cash REQUIRED]",
                    Conversation = clonedConversation,
                    Enabled = false,
                    Priority = 99,
                    onChoosen = new UnityEngine.Events.UnityEvent()
                };

                clonedConversation.DialogueNodeData[0].DialogueText = "I bet. You can try the motel if you want a decent place to stay, talk to Donna. Its pretty pricy though.";
                clonedConversation.DialogueNodeData[1].DialogueText = "Uh sure... I can fix the body but I don't think she'll ever run again";
                clonedConversation.DialogueNodeData[1].choices[0].ChoiceText = "Yeah, I don't think I'm going anywhere for a while...";
                if (clonedConversation.DialogueNodeData[1].choices.Count > 1)
                    clonedConversation.DialogueNodeData[1].choices.RemoveAt(1);

                newChoice.onChoosen.AddListener((UnityEngine.Events.UnityAction)MarcoRentRoomAction);
                marcoController.Choices.Add(newChoice);

                MelonCoroutines.Start(WaitForQuestCompletionAndEnableChoice(questTitle, marcoController, newChoice));
                Melon<ProperPropertyProgression>.Logger.Msg("Added rent-room choice to Marco with a cloned conversation.");
            }

            private static IEnumerator WaitForQuestCompletionAndEnableChoice(string questTitle, DialogueController controller, DialogueController.DialogueChoice choice)
            {
                EQuestState? previousState = null;

                while (true)
                {
                    var quest = UnityEngine.Object.FindObjectsOfType<QuestEntry>().FirstOrDefault(q => q.Title == questTitle);
                    if (quest != null && quest.state != previousState)
                    {
                        previousState = quest.state;
                        choice.Enabled = quest.state == EQuestState.Completed;

                        var filtered = new Il2CppSystem.Collections.Generic.List<DialogueController.DialogueChoice>();
                        foreach (var c in controller.Choices)
                            if (c.Enabled) filtered.Add(c);

                        var shownChoicesField = typeof(DialogueController).GetField("shownChoices", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        shownChoicesField?.SetValue(controller, filtered);
                    }

                    yield return new WaitForSeconds(1f);
                    if (completedRV || (isRVFixed() && !CheckFirstQuest())){

                        choice.Enabled = false;
                        Melon<ProperPropertyProgression>.Logger.Msg("RV is fixed and first quest is completed. Quiting routine.");
                        yield break;
                        }
                }
            }

            public static void MarcoRentRoomAction()
            {
                var marco = GameObject.Find("Marco");
                var cash = GetClosestPlayersCashInstance(marco);

                if (cash != null && cash.Balance >= ModConfig.RVPrice.Value)
                {
                    cash.ChangeBalance(-ModConfig.RVPrice.Value);
                    SetQuestInactive("Head back to the RV");
                    SetQuestInactive("Investigate the explosion");
                    SetQuestInactive("Read the note");
                    if (!ModConfig.StartRVEmpty.Value)ClearRVItemContainers();
                    SetRVActive();
                    UnlockAlbertNow();
                    Melon<ProperPropertyProgression>.Logger.Msg($"${ModConfig.RVPrice.Value} deducted and RV restored.");
                    completedRV = true;
                }
            }
        }
        public static void DisableQuestExplosions()
        {
            foreach (var obj in UnityEngine.Resources.FindObjectsOfTypeAll(Il2CppType.Of<Il2CppScheduleOne.Quests.Quest_WelcomeToHylandPoint>()))
            {
                var quest = obj.TryCast<Il2CppScheduleOne.Quests.Quest_WelcomeToHylandPoint>();
                if (quest != null && quest.onExplode != null)
                {
                    quest.onExplode = new UnityEngine.Events.UnityEvent(); // replaces only this field
                    Melon<ProperPropertyProgression>.Logger.Msg("onExplode disabled for Quest_WelcomeToHylandPoint.");
                }
            }
        }
        public static void SetRVActive()
        {
            GameObject rvContainer = null, intactRV = null, destroyedRV = null;

            foreach (var obj in UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (obj.name == "RV" && obj.transform?.parent?.name == "@Properties")
                {
                    rvContainer = obj;
                    break;
                }
            }

            if (rvContainer == null) return;

            foreach (var t in rvContainer.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "RV" && t.gameObject != rvContainer) intactRV = t.gameObject;
                else if (t.name == "Destroyed RV") destroyedRV = t.gameObject;
            }

            if (intactRV != null && destroyedRV != null)
            {
                destroyedRV.SetActive(false);
                intactRV.SetActive(true);
            }

            Melon<ProperPropertyProgression>.Logger.Msg("RV is now visible.");
        }


        public static bool isRVFixed()
        {
            GameObject rvContainer = null, intactRV = null, destroyedRV = null;

            foreach (var obj in UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (obj.name == "RV" && obj.transform?.parent?.name == "@Properties")
                {
                    rvContainer = obj;
                    break;
                }
            }

            foreach (var t in rvContainer.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "RV" && t.gameObject != rvContainer) intactRV = t.gameObject;
                else if (t.name == "Destroyed RV") destroyedRV = t.gameObject;
            }

            if (intactRV != null && destroyedRV != null)
            {
                if (intactRV.active && !destroyedRV.active) return true; else return false;
            }
            return false;

        }
        public static bool CheckFirstQuest()
        {
            const string questTitle = "Open your phone and read your messages";

            foreach (var entry in UnityEngine.Object.FindObjectsOfType<Il2CppScheduleOne.Quests.QuestEntry>())
            {
                if (entry?.Title?.Trim() == questTitle)
                {
                    bool shouldClear = entry.state != Il2CppScheduleOne.Quests.EQuestState.Completed;
                    Melon<ProperPropertyProgression>.Logger.Msg($"Quest '{questTitle}' is {entry.state} => shouldClear = {shouldClear}");
                    return shouldClear;
                }
            }

            Melon<ProperPropertyProgression>.Logger.Warning("First quest not found — defaulting to shouldClear = true");
            return true;
        }

        public static void ClearRVItemContainers()
        {
            GameObject rvContainer = null;

            foreach (var obj in UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (obj.name == "RV" && obj.transform?.parent?.name == "RV" && obj.transform?.parent?.parent?.name == "@Properties")
                {
                    var container = obj.transform.Find("Container");
                    if (container != null)
                    {
                        rvContainer = container.gameObject;
                        break;
                    }
                }
            }

            if (rvContainer == null)
                return;

            int destroyed = 0;
            foreach (var child in rvContainer.GetComponentsInChildren<Transform>(true))
            {
                if (child.parent.gameObject != rvContainer) continue;

                if (child.name == "Grid" || child.name == "Grid (1)")
                {
                    var gridObj = child.gameObject;

                    foreach (var obj in UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>())
                    {
                        if (obj.name == "LEDLight(Clone)")
                        {
                            var current = obj.transform;
                            while (current != null)
                            {
                                if (current.gameObject == gridObj)
                                {
                                    var growLight = obj.GetComponent<Il2CppScheduleOne.ObjectScripts.GrowLight>();
                                    if (growLight != null)
                                    {
                                        growLight.DestroyItem(true);
                                    }
                                    break;
                                }
                                current = current.parent;
                            }
                        }
                    }

                    var gridComp = gridObj.GetComponent<Il2CppScheduleOne.Tiles.Grid>();
                    if (gridComp != null)
                    {
                        gridComp.DestroyGrid();
                        destroyed++;

                    }
                }
            }
        }


    






        public static Il2CppScheduleOne.ItemFramework.CashInstance GetClosestPlayersCashInstance(GameObject npc)
        {
            var players = UnityEngine.Object.FindObjectsOfType<Il2CppScheduleOne.PlayerScripts.Player>();
            Il2CppScheduleOne.PlayerScripts.Player closestPlayer = null;
            float closestDist = float.MaxValue;
            var npcPos = npc.GetComponent<Transform>()?.position ?? Vector3.zero;

            foreach (var player in players)
            {
                var playerGO = player.TryCast<UnityEngine.Component>()?.gameObject;
                if (playerGO == null) continue;

                var playerPos = playerGO.GetComponent<Transform>()?.position ?? Vector3.zero;
                float dist = Vector3.Distance(playerPos, npcPos);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestPlayer = player;
                }
            }

            if (closestPlayer == null) return null;

            var finalGO = closestPlayer.TryCast<UnityEngine.Component>()?.gameObject;
            if (finalGO == null) return null;

            var inventory = finalGO.GetComponentsInChildren<Il2CppScheduleOne.PlayerScripts.PlayerInventory>(true).FirstOrDefault();
            return inventory?.cashInstance;
        }
    }
}
