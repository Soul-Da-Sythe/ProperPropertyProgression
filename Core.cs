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

[assembly: MelonInfo(typeof(ProperPropertyProgression.ProperPropertyProgression), "ProperPropertyProgression", "1.0.0", "Soul", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ProperPropertyProgression
{
    public class ProperPropertyProgression : MelonMod
    {
        public static class ModConfig
        {
            private static MelonPreferences_Category PropertyChanges;
            public static MelonPreferences_Entry<bool> ChangeHousePrices;
            public static MelonPreferences_Entry<int> MotelPrice;
            public static MelonPreferences_Entry<int> SweatshopPrice;
            public static MelonPreferences_Entry<int> BungalowPrice;

            private static MelonPreferences_Category RVChanges;
            public static MelonPreferences_Entry<bool> StartRVEmpty;
            public static MelonPreferences_Entry<int> RVPrice;

            public static void Setup()
            {
                PropertyChanges = MelonPreferences.CreateCategory("ProperPropertyProgression", "Proper Property Progression Changes", true);
                ChangeHousePrices = PropertyChanges.CreateEntry("ChangeHousePrices", true, "Change house prices.", "Changes the prices of the Hotel, the Sweatshop, and the Bungalow.");
                MotelPrice = PropertyChanges.CreateEntry("MotelPrice", 2500, "Hotel Price");
                SweatshopPrice = PropertyChanges.CreateEntry("SweatshopPrice", 5000, "Sweatshop Price");
                BungalowPrice = PropertyChanges.CreateEntry("BungalowPrice", 15000, "Bungalow Price");

                RVChanges = MelonPreferences.CreateCategory("ProperPropertyProgression", "RVChanges", true);
                StartRVEmpty = RVChanges.CreateEntry("StartRVEmpty", true, "Start RV Empty", "Deletes everything in the RV at the start. (Note, after the RV is destroyed everything is still deleted before you repair it. This just ensure you can't cheese by picking everything up before.)");
                RVPrice = RVChanges.CreateEntry("RVPrice", 500, "RV Price");

                PropertyChanges.SaveToFile(false);
                RVChanges.SaveToFile(false);
            }
        }

        public override void OnInitializeMelon()
        {
            ModConfig.Setup();
            LoggerInstance.Msg("Config initialized.");
            Melon<ProperPropertyProgression>.Logger.Msg("Initialized!");
            SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>)OnSceneLoaded);
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
                DialogueModifier.SetAllBungalowPrices(ModConfig.BungalowPrice.Value);
            }

            DialogueModifier.SetQuestActive("Talk to the manager in the motel office");
            DialogueModifier.SetQuestActive("Talk to Mrs. Ming at the Chinese restaurant");

            if (ShouldClearRVBasedOnFirstQuest()) ClearRVItemContainers();
            DialogueModifier.SetupMarcoRentRoomChoice();
        }

        public static class DialogueModifier
        {
            private static Dictionary<string, int> npcPriceMap = new()
            {
                { "Donna", ModConfig.MotelPrice.Value },
                { "Ming", ModConfig.SweatshopPrice.Value },
            };

            public static void ApplyPatch()
            {
                var allControllers = GameObject.FindObjectsOfType<DialogueController_Ming>();
                foreach (var controller in allControllers)
                {
                    var parentName = controller.gameObject.transform.parent?.name;
                    if (parentName != null && npcPriceMap.TryGetValue(parentName, out int newPrice))
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

            public static void SetAllBungalowPrices(int newPrice)
            {
                foreach (var bungalow in UnityEngine.Object.FindObjectsOfType<Bungalow>())
                {
                    bungalow.Price = newPrice;
                }

                Melon<ProperPropertyProgression>.Logger.Msg("Bungalow prices updated.");
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
                    ClearRVItemContainers();
                    SetRVActive();
                    Melon<ProperPropertyProgression>.Logger.Msg($"${ModConfig.RVPrice.Value} deducted and RV restored.");
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

        public static bool ShouldClearRVBasedOnFirstQuest()
        {
            const string questTitle = "Open your phone (press Tab) and read your messages";

            foreach (var entry in UnityEngine.Object.FindObjectsOfType<Il2CppScheduleOne.Quests.QuestEntry>())
            {
                if (entry.Title == questTitle)
                {
                    bool shouldClear = entry.state != Il2CppScheduleOne.Quests.EQuestState.Completed;
                    return shouldClear;
                }
            }
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

            Melon<ProperPropertyProgression>.Logger.Msg($"Called DestroyGrid() on {destroyed} Grid objects.");
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
