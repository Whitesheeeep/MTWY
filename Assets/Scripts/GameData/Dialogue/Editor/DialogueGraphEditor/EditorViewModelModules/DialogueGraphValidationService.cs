using System.Collections.Generic;
using System.Linq;

namespace GameData.Editor
{
    /// <summary>
    /// 对话图校验服务，负责生成编辑器校验提示。
    /// </summary>
    internal sealed class DialogueGraphValidationService
    {
        #region 校验入口
        /// <summary>
        /// 校验对话图并返回所有提示消息。
        /// </summary>
        public List<DialogueGraphValidationMessage> Validate(
            DialogueGraph_SO graph,
            DialogueGraphConnectionService connectionService)
        {
            List<DialogueGraphValidationMessage> messages = new();

            if (graph == null)
            {
                return messages;
            }

            graph.RemoveNullNodes();
            ValidateStartNode(graph, messages);
            ValidateSpeechNodes(graph, connectionService, messages);
            ValidateChoiceNodes(graph, connectionService, messages);
            ValidateSpeakerDataList(messages);
            return messages;
        }
        #endregion

        #region 节点校验
        private static void ValidateStartNode(DialogueGraph_SO graph, ICollection<DialogueGraphValidationMessage> messages)
        {
            if (graph.StartNode == null)
            {
                messages.Add(new DialogueGraphValidationMessage("Missing Start node."));
            }
            else if (graph.StartNode.NextNode == null)
            {
                messages.Add(new DialogueGraphValidationMessage("Start node has no target Speech node."));
            }
        }

        private static void ValidateSpeechNodes(
            DialogueGraph_SO graph,
            DialogueGraphConnectionService connectionService,
            ICollection<DialogueGraphValidationMessage> messages)
        {
            foreach (DialogueSpeechNode speech in graph.EnumerateNodes().OfType<DialogueSpeechNode>())
            {
                if (string.IsNullOrWhiteSpace(speech.SpeakerId))
                {
                    messages.Add(new DialogueGraphValidationMessage($"{GetDisplayName(speech)} has no Speaker."));
                }
                else if (DialogueSpeakerDataListLocator.FindSpeaker(speech.SpeakerId) == null)
                {
                    messages.Add(new DialogueGraphValidationMessage($"{GetDisplayName(speech)} references missing Speaker '{speech.SpeakerId}'."));
                }
                else
                {
                    DialogueSpeakerData speaker = DialogueSpeakerDataListLocator.FindSpeaker(speech.SpeakerId);
                    if (!string.IsNullOrWhiteSpace(speech.PortraitId) && !SpeakerHasPortraitId(speaker, speech.PortraitId))
                    {
                        messages.Add(new DialogueGraphValidationMessage($"{GetDisplayName(speech)} references missing Portrait '{speech.PortraitId}' on Speaker '{speech.SpeakerId}'."));
                    }
                }

                bool hasChoices = connectionService.GetChoicesFrom(speech).Any();
                if (speech.NextNode == null && !hasChoices)
                {
                    messages.Add(new DialogueGraphValidationMessage($"{GetDisplayName(speech)} has no next node or Choice branch."));
                }

                if (speech.NextNode != null && hasChoices)
                {
                    messages.Add(new DialogueGraphValidationMessage($"{GetDisplayName(speech)} has both a linear next node and Choice branches."));
                }
            }
        }

        private static void ValidateChoiceNodes(
            DialogueGraph_SO graph,
            DialogueGraphConnectionService connectionService,
            ICollection<DialogueGraphValidationMessage> messages)
        {
            foreach (DialogueChoiceNode choice in graph.EnumerateNodes().OfType<DialogueChoiceNode>())
            {
                if (!connectionService.IsChoiceOwned(graph, choice))
                {
                    messages.Add(new DialogueGraphValidationMessage($"{GetDisplayName(choice)} has no source Speech node."));
                }

                if (choice.TargetNode == null)
                {
                    messages.Add(new DialogueGraphValidationMessage($"{GetDisplayName(choice)} has no target node."));
                }
            }
        }
        #endregion

        #region Speaker 校验
        private static void ValidateSpeakerDataList(ICollection<DialogueGraphValidationMessage> messages)
        {
            int dataListCount = DialogueSpeakerDataListLocator.GetDataListCount();
            if (dataListCount == 0)
            {
                messages.Add(new DialogueGraphValidationMessage("Missing DialogueSpeakerDataList_SO asset."));
                return;
            }

            if (dataListCount > 1)
            {
                messages.Add(new DialogueGraphValidationMessage("Multiple DialogueSpeakerDataList_SO assets found. The editor uses the first one."));
            }

            DialogueSpeakerDataList_SO dataList = DialogueSpeakerDataListLocator.GetDataList();
            if (dataList?.items == null)
            {
                return;
            }

            foreach (IGrouping<string, DialogueSpeakerData> duplicateGroup in dataList.items
                         .Where(item => item != null && !string.IsNullOrWhiteSpace(item.speakerId))
                         .GroupBy(item => item.speakerId)
                         .Where(group => group.Count() > 1))
            {
                messages.Add(new DialogueGraphValidationMessage($"Duplicate SpeakerId '{duplicateGroup.Key}' in DialogueSpeakerDataList_SO."));
            }

            foreach (DialogueSpeakerData speaker in dataList.items.Where(item => item != null))
            {
                if (string.IsNullOrWhiteSpace(speaker.speakerId))
                {
                    messages.Add(new DialogueGraphValidationMessage("DialogueSpeakerDataList_SO contains a Speaker with empty speakerId."));
                }

                if (string.IsNullOrWhiteSpace(speaker.portraitAtlasAddress) && speaker.portraitIds != null && speaker.portraitIds.Any(item => !string.IsNullOrWhiteSpace(item)))
                {
                    messages.Add(new DialogueGraphValidationMessage($"{speaker.speakerId} has portraitIds but no portraitAtlasAddress."));
                }

                if (speaker.portraitIds == null)
                {
                    continue;
                }

                if (speaker.portraitIds.Any(string.IsNullOrWhiteSpace))
                {
                    messages.Add(new DialogueGraphValidationMessage($"{speaker.speakerId} has empty portraitId."));
                }

                foreach (IGrouping<string, string> duplicateGroup in speaker.portraitIds
                             .Where(item => !string.IsNullOrWhiteSpace(item))
                             .GroupBy(item => item)
                             .Where(group => group.Count() > 1))
                {
                    messages.Add(new DialogueGraphValidationMessage($"{speaker.speakerId} has duplicate portraitId '{duplicateGroup.Key}'."));
                }

                if (!string.IsNullOrWhiteSpace(speaker.defaultPortraitId) && !SpeakerHasPortraitId(speaker, speaker.defaultPortraitId))
                {
                    messages.Add(new DialogueGraphValidationMessage($"{speaker.speakerId} defaultPortraitId '{speaker.defaultPortraitId}' is not in portraitIds."));
                }
            }
        }
        #endregion

        #region 工具方法
        private static string GetDisplayName(DialogueNode node)
        {
            if (node == null)
            {
                return "Missing node";
            }

            if (!string.IsNullOrWhiteSpace(node.EditorTitle))
            {
                return node.EditorTitle;
            }

            return node.name;
        }

        private static bool SpeakerHasPortraitId(DialogueSpeakerData speaker, string portraitId)
        {
            return speaker?.portraitIds != null &&
                   !string.IsNullOrWhiteSpace(portraitId) &&
                   speaker.portraitIds.Contains(portraitId);
        }
        #endregion
    }
}
