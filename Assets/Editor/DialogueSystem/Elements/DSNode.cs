using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DS.Elements
{
    using Data.Save;
    using Enumerations;
    using Utilities;
    using Windows;

    public class DSNode : Node
    {
        private const string RoleSpriteAtlasPath = "Assets/Art/Role/SpriteAtlas.spriteatlasv2";

        public string ID { get; set; }
        public string DialogueName { get; set; }
        public List<DSChoiceSaveData> Choices { get; set; }
        public string Text { get; set; }
        public DSDialogueType DialogueType { get; set; }
        public DSDialogueSpeaker Speaker { get; set; }
        public string SpeakerExpressionPath { get; set; }
        public DSGroup Group { get; set; }

        protected DSGraphView graphView;
        private Color defaultBackgroundColor;

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Disconnect Input Ports", actionEvent => DisconnectInputPorts());
            evt.menu.AppendAction("Disconnect Output Ports", actionEvent => DisconnectOutputPorts());

            base.BuildContextualMenu(evt);
        }

        public virtual void Initialize(string nodeName, DSGraphView dsGraphView, Vector2 position)
        {
            ID = Guid.NewGuid().ToString();

            DialogueName = nodeName;
            Choices = new List<DSChoiceSaveData>();
            Text = "Dialogue text.";
            Speaker = DSDialogueSpeaker.Me;
            SpeakerExpressionPath = GetDefaultExpressionPath(Speaker);

            SetPosition(new Rect(position, Vector2.zero));

            graphView = dsGraphView;
            defaultBackgroundColor = new Color(29f / 255f, 29f / 255f, 30f / 255f);

            mainContainer.AddToClassList("ds-node__main-container");
            extensionContainer.AddToClassList("ds-node__extension-container");
        }

        public virtual void Draw()
        {
            /* TITLE CONTAINER */

            TextField dialogueNameTextField = DSElementUtility.CreateTextField(DialogueName, null, callback =>
            {
                TextField target = (TextField) callback.target;

                target.value = callback.newValue.RemoveWhitespaces().RemoveSpecialCharacters();

                if (string.IsNullOrEmpty(target.value))
                {
                    if (!string.IsNullOrEmpty(DialogueName))
                    {
                        ++graphView.NameErrorsAmount;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(DialogueName))
                    {
                        --graphView.NameErrorsAmount;
                    }
                }

                if (Group == null)
                {
                    graphView.RemoveUngroupedNode(this);

                    DialogueName = target.value;

                    graphView.AddUngroupedNode(this);

                    return;
                }

                DSGroup currentGroup = Group;

                graphView.RemoveGroupedNode(this, Group);

                DialogueName = target.value;

                graphView.AddGroupedNode(this, currentGroup);
            });

            dialogueNameTextField.AddClasses(
                "ds-node__text-field",
                "ds-node__text-field__hidden",
                "ds-node__filename-text-field"
            );

            titleContainer.Insert(0, dialogueNameTextField);

            /* INPUT CONTAINER */

            Port inputPort = this.CreatePort("Dialogue Connection", Orientation.Horizontal, Direction.Input, Port.Capacity.Multi);

            inputContainer.Add(inputPort);

            /* EXTENSION CONTAINER */

            VisualElement customDataContainer = new VisualElement();

            customDataContainer.AddToClassList("ds-node__custom-data-container");

            Foldout textFoldout = DSElementUtility.CreateFoldout("Dialogue Text");

            TextField textTextField = DSElementUtility.CreateTextArea(Text, null, callback => Text = callback.newValue);

            textTextField.AddClasses(
                "ds-node__text-field",
                "ds-node__quote-text-field"
            );

            textFoldout.Add(textTextField);

            customDataContainer.Add(textFoldout);

            if (DialogueType == DSDialogueType.SingleChoice)
            {
                List<string> currentExpressionReferences = GetExpressionReferences(Speaker);
                SpeakerExpressionPath = ResolveExpressionPath(
                    SpeakerExpressionPath,
                    currentExpressionReferences);
                List<string> expressionNames = currentExpressionReferences
                    .Select(GetExpressionDisplayName)
                    .ToList();
                if (expressionNames.Count == 0)
                {
                    expressionNames.Add("(None)");
                }

                PopupField<string> expressionField = new PopupField<string>(
                    "Expression",
                    expressionNames,
                    currentExpressionReferences.Count == 0
                        ? 0
                        : GetExpressionIndex(SpeakerExpressionPath, currentExpressionReferences));
                expressionField.RegisterValueChangedCallback(callback =>
                {
                    if (currentExpressionReferences.Count == 0)
                    {
                        SpeakerExpressionPath = string.Empty;
                        return;
                    }

                    int expressionIndex = expressionNames.IndexOf(callback.newValue);
                    SpeakerExpressionPath = expressionIndex >= 0 && expressionIndex < currentExpressionReferences.Count
                        ? currentExpressionReferences[expressionIndex]
                        : string.Empty;
                });

                List<string> speakerOptions = new List<string> { "我", "女朋友", "女儿" };
                PopupField<string> speakerField = new PopupField<string>(
                    "Speaker",
                    speakerOptions,
                    GetSpeakerIndex(Speaker));
                speakerField.RegisterValueChangedCallback(callback =>
                {
                    Speaker = GetSpeakerByOption(callback.newValue);

                    currentExpressionReferences = GetExpressionReferences(Speaker);
                    SpeakerExpressionPath = currentExpressionReferences.Count == 0
                        ? string.Empty
                        : currentExpressionReferences[0];

                    expressionNames = currentExpressionReferences
                        .Select(GetExpressionDisplayName)
                        .ToList();
                    if (expressionNames.Count == 0)
                    {
                        expressionNames.Add("(None)");
                    }
                    expressionField.choices = expressionNames;
                    expressionField.index = 0;
                    expressionField.SetValueWithoutNotify(expressionNames[0]);
                });
                customDataContainer.Add(speakerField);
                customDataContainer.Add(expressionField);
            }

            extensionContainer.Add(customDataContainer);
        }

        public void DisconnectAllPorts()
        {
            DisconnectInputPorts();
            DisconnectOutputPorts();
        }

        private void DisconnectInputPorts()
        {
            DisconnectPorts(inputContainer);
        }

        private void DisconnectOutputPorts()
        {
            DisconnectPorts(outputContainer);
        }

        private void DisconnectPorts(VisualElement container)
        {
            foreach (Port port in container.Children())
            {
                if (!port.connected)
                {
                    continue;
                }

                graphView.DeleteElements(port.connections);
            }
        }

        public bool IsStartingNode()
        {
            Port inputPort = (Port) inputContainer.Children().First();

            return !inputPort.connected;
        }

        public void SetErrorStyle(Color color)
        {
            mainContainer.style.backgroundColor = color;
        }

        public void ResetStyle()
        {
            mainContainer.style.backgroundColor = defaultBackgroundColor;
        }

        private static int GetSpeakerIndex(DSDialogueSpeaker speaker)
        {
            switch (speaker)
            {
                case DSDialogueSpeaker.Girlfriend:
                    return 1;
                case DSDialogueSpeaker.Daughter:
                    return 2;
                default:
                    return 0;
            }
        }

        private static DSDialogueSpeaker GetSpeakerByOption(string option)
        {
            switch (option)
            {
                case "女朋友":
                    return DSDialogueSpeaker.Girlfriend;
                case "女儿":
                    return DSDialogueSpeaker.Daughter;
                default:
                    return DSDialogueSpeaker.Me;
            }
        }

        private static List<string> GetExpressionReferences(DSDialogueSpeaker speaker)
        {
            string folderPath = $"Assets/Art/Role/Sprites/{GetSpeakerFolderName(speaker)}";
            string[] expressionGuids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
            List<string> expressionReferences = new List<string>(expressionGuids.Length);
            for (int i = 0; i < expressionGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(expressionGuids[i]);
                expressionReferences.Add(BuildAtlasExpressionReference(assetPath));
            }

            expressionReferences.Sort(StringComparer.Ordinal);
            return expressionReferences;
        }

        private static string GetDefaultExpressionPath(DSDialogueSpeaker speaker)
        {
            List<string> expressionReferences = GetExpressionReferences(speaker);
            return expressionReferences.Count == 0 ? string.Empty : expressionReferences[0];
        }

        private static string ResolveExpressionPath(string candidatePath, List<string> expressionReferences)
        {
            if (expressionReferences == null || expressionReferences.Count == 0)
            {
                return string.Empty;
            }

            string normalizedCandidate = NormalizeExpressionReference(candidatePath);
            if (!string.IsNullOrEmpty(normalizedCandidate) && expressionReferences.Contains(normalizedCandidate))
            {
                return normalizedCandidate;
            }

            return expressionReferences[0];
        }

        private static int GetExpressionIndex(string expressionPath, List<string> expressionReferences)
        {
            int index = expressionReferences.IndexOf(expressionPath);
            return index < 0 ? 0 : index;
        }

        private static string BuildAtlasExpressionReference(string assetPath)
        {
            string spriteName = Path.GetFileNameWithoutExtension(assetPath);
            return string.IsNullOrEmpty(spriteName)
                ? string.Empty
                : $"{RoleSpriteAtlasPath}[{spriteName}]";
        }

        private static string NormalizeExpressionReference(string expressionPath)
        {
            if (string.IsNullOrEmpty(expressionPath))
            {
                return string.Empty;
            }

            if (expressionPath.Contains(".spriteatlasv2[") && expressionPath.EndsWith("]"))
            {
                return expressionPath;
            }

            return BuildAtlasExpressionReference(expressionPath);
        }

        private static string GetExpressionDisplayName(string expressionReference)
        {
            if (string.IsNullOrEmpty(expressionReference))
            {
                return string.Empty;
            }

            int leftBracket = expressionReference.IndexOf('[');
            int rightBracket = expressionReference.LastIndexOf(']');
            if (leftBracket < 0 || rightBracket <= leftBracket)
            {
                return Path.GetFileNameWithoutExtension(expressionReference);
            }

            return expressionReference.Substring(leftBracket + 1, rightBracket - leftBracket - 1);
        }

        private static string GetSpeakerFolderName(DSDialogueSpeaker speaker)
        {
            switch (speaker)
            {
                case DSDialogueSpeaker.Girlfriend:
                    return "girlfriend";
                case DSDialogueSpeaker.Daughter:
                    return "daughter";
                default:
                    return "me";
            }
        }
    }
}