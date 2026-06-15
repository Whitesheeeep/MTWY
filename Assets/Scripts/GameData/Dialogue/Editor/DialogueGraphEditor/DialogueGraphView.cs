using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameData.Editor
{
    /// <summary>
    /// 对话图 GraphView，负责节点视图、连线交互、右键菜单、布局操作和运行时节点高亮。
    /// </summary>
    internal sealed class DialogueGraphView : GraphView
    {
        #region 字段
        private readonly DialogueGraphEditorViewModel viewModel;
        private bool isPopulating;
        private bool isHandlingGraphChange;
        private bool pendingPopulate;
        private DialogueNode runtimeCurrentNode;
        #endregion

        #region 初始化
        /// <summary>
        /// 创建对话图 GraphView 并配置基础交互器。
        /// </summary>
        /// <param name="viewModel">对话图编辑器 ViewModel。</param>
        public DialogueGraphView(DialogueGraphEditorViewModel viewModel)
        {
            this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            AddToClassList("dialogue-graph-view");
            Insert(0, new GridBackground());

            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            graphViewChanged += OnGraphViewChanged;
        }
        #endregion

        #region 构建与刷新
        /// <summary>
        /// 根据当前 Graph 数据重建所有节点视图和连线。
        /// </summary>
        public void Populate()
        {
            if (isHandlingGraphChange)
            {
                RequestPopulate();
                return;
            }

            isPopulating = true;

            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(edges.ToList());
            DeleteElements(nodes.ToList());
            graphViewChanged += OnGraphViewChanged;

            DialogueGraph_SO graph = viewModel.Graph;
            if (graph == null)
            {
                isPopulating = false;
                return;
            }

            foreach (DialogueNode node in graph.EnumerateNodes())
            {
                AddElement(CreateNodeView(node));
            }

            foreach (DialogueNode node in graph.EnumerateNodes())
            {
                CreateEdges(node);
            }

            isPopulating = false;
            ApplyRuntimeCurrentNodeClass();
        }

        /// <summary>
        /// 请求重建 GraphView；如果正处于 GraphViewChange 回调中，则延迟到当前事件结束后执行。
        /// </summary>
        public void RequestPopulate()
        {
            if (!isHandlingGraphChange)
            {
                Populate();
                return;
            }

            if (pendingPopulate)
            {
                return;
            }

            pendingPopulate = true;
            schedule.Execute(FlushPendingPopulate);
        }

        /// <summary>
        /// 获取当前选中的对话节点数据。
        /// </summary>
        /// <returns>当前选中的对话节点数据列表。</returns>
        public IReadOnlyList<DialogueNode> GetSelectedDialogueNodes()
        {
            return selection
                .OfType<DialogueNodeView>()
                .Select(nodeView => nodeView.DialogueNode)
                .Where(node => node != null)
                .ToList();
        }

        /// <summary>
        /// 根据节点数据刷新当前存在的节点视图。
        /// </summary>
        public void RefreshNodeViews()
        {
            foreach (UnityEditor.Experimental.GraphView.Node node in nodes.ToList())
            {
                if (node is DialogueNodeView nodeView)
                {
                    nodeView.RefreshFromData();
                }
            }

            ApplyRuntimeCurrentNodeClass();
        }
        #endregion

        #region 运行时高亮
        /// <summary>
        /// 设置运行时当前节点高亮，供编辑器窗口在播放模式下轮询 Runner 状态时调用。
        /// </summary>
        /// <param name="node">当前运行到的对话节点；传入空值时清空高亮。</param>
        public void SetRuntimeCurrentNode(DialogueNode node)
        {
            if (runtimeCurrentNode == node)
            {
                return;
            }

            runtimeCurrentNode = node;
            ApplyRuntimeCurrentNodeClass();
        }

        private void ApplyRuntimeCurrentNodeClass()
        {
            foreach (UnityEditor.Experimental.GraphView.Node node in nodes.ToList())
            {
                if (node is DialogueNodeView nodeView)
                {
                    nodeView.EnableInClassList("dialogue-node-runtime-current", nodeView.DialogueNode == runtimeCurrentNode);
                }
            }
        }
        #endregion

        #region GraphView 重写
        /// <summary>
        /// 获取可连接端口列表，并把连接规则委托给 ViewModel 判断。
        /// </summary>
        /// <param name="startPort">正在拖拽的起始端口。</param>
        /// <param name="nodeAdapter">GraphView 传入的节点适配器。</param>
        /// <returns>当前可连接的端口列表。</returns>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            if (startPort?.node is not DialogueNodeView startNodeView)
            {
                return new List<Port>();
            }

            return ports
                .Where(port => port != startPort)
                .Where(port => port.direction != startPort.direction)
                .Where(port => port.node != startPort.node)
                .Where(port => port.node is DialogueNodeView)
                .Where(port =>
                {
                    DialogueNodeView endNodeView = (DialogueNodeView)port.node;
                    DialogueNode outputNode = startPort.direction == Direction.Output
                        ? startNodeView.DialogueNode
                        : endNodeView.DialogueNode;
                    DialogueNode inputNode = startPort.direction == Direction.Output
                        ? endNodeView.DialogueNode
                        : startNodeView.DialogueNode;
                    return viewModel.CanConnect(outputNode, inputNode);
                })
                .ToList();
        }

        /// <summary>
        /// GraphView 完成添加选择后同步 ViewModel 选中节点，避免 MouseDown 早于内部选择状态。
        /// </summary>
        /// <param name="selectable">新加入选择集的元素。</param>
        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);
            SyncSelectedNodeToViewModel();
        }

        /// <summary>
        /// GraphView 完成移除选择后同步 ViewModel 选中节点。
        /// </summary>
        /// <param name="selectable">被移出选择集的元素。</param>
        public override void RemoveFromSelection(ISelectable selectable)
        {
            base.RemoveFromSelection(selectable);
            SyncSelectedNodeToViewModel();
        }

        /// <summary>
        /// GraphView 清空选择后同步 ViewModel 选中节点。
        /// </summary>
        public override void ClearSelection()
        {
            base.ClearSelection();
            SyncSelectedNodeToViewModel();
        }

        /// <summary>
        /// 构建右键菜单，包含创建节点和选中节点布局命令。
        /// </summary>
        /// <param name="evt">右键菜单构建事件。</param>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (viewModel.Graph == null)
            {
                return;
            }

            Vector2 position = contentViewContainer.WorldToLocal(evt.mousePosition);
            evt.menu.AppendAction("Create/Speech Node", _ => CreateNode(typeof(DialogueSpeechNode), position));
            evt.menu.AppendAction("Create/Choice Node", _ => CreateNode(typeof(DialogueChoiceNode), position));
            evt.menu.AppendAction("Create/End Node", _ => CreateNode(typeof(DialogueEndNode), position));
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Layout/Horizontal/Align Top", _ => AlignSelectedNodesTop(), GetAlignMenuStatus);
            evt.menu.AppendAction("Layout/Horizontal/Align Middle", _ => AlignSelectedNodesMiddle(), GetAlignMenuStatus);
            evt.menu.AppendAction("Layout/Horizontal/Align Bottom", _ => AlignSelectedNodesBottom(), GetAlignMenuStatus);
            evt.menu.AppendAction("Layout/Horizontal/Distribute Evenly", _ => DistributeSelectedNodesHorizontally(), GetDistributeMenuStatus);
            evt.menu.AppendAction("Layout/Vertical/Align Left", _ => AlignSelectedNodesLeft(), GetAlignMenuStatus);
            evt.menu.AppendAction("Layout/Vertical/Align Center", _ => AlignSelectedNodesCenter(), GetAlignMenuStatus);
            evt.menu.AppendAction("Layout/Vertical/Align Right", _ => AlignSelectedNodesRight(), GetAlignMenuStatus);
            evt.menu.AppendAction("Layout/Vertical/Distribute Evenly", _ => DistributeSelectedNodesVertically(), GetDistributeMenuStatus);
        }
        #endregion

        #region 布局
        /// <summary>
        /// 根据图连接层级和当前节点视图真实尺寸自动排版，避免同层节点互相遮挡。
        /// </summary>
        public void AutoLayoutGraph()
        {
            if (viewModel.Graph == null)
            {
                return;
            }

            const float startX = 80f;
            const float centerY = 320f;
            const float layerGap = 120f;
            const float nodeGap = 48f;
            const float unreachableOffsetY = 220f;

            Dictionary<DialogueNode, int> layerByNode = viewModel.CalculateAutoLayoutLayers();
            List<DialogueNodeView> allNodeViews = nodes
                .OfType<DialogueNodeView>()
                .Where(nodeView => nodeView.DialogueNode != null)
                .ToList();

            if (allNodeViews.Count == 0)
            {
                return;
            }

            float currentX = startX;
            foreach (IGrouping<int, DialogueNodeView> layerGroup in allNodeViews
                         .Where(nodeView => layerByNode.ContainsKey(nodeView.DialogueNode))
                         .GroupBy(nodeView => layerByNode[nodeView.DialogueNode])
                         .OrderBy(group => group.Key))
            {
                List<DialogueNodeView> layerNodeViews = layerGroup
                    .OrderBy(nodeView => nodeView.GetPosition().y)
                    .ThenBy(nodeView => nodeView.GetPosition().x)
                    .ToList();

                float layerWidth = LayoutNodeViewColumn(
                    layerNodeViews,
                    currentX,
                    centerY,
                    nodeGap,
                    "Auto Layout Dialogue Graph");

                currentX += layerWidth + layerGap;
            }

            List<DialogueNodeView> unreachableNodeViews = allNodeViews
                .Where(nodeView => !layerByNode.ContainsKey(nodeView.DialogueNode))
                .OrderBy(nodeView => nodeView.GetPosition().x)
                .ThenBy(nodeView => nodeView.GetPosition().y)
                .ToList();

            if (unreachableNodeViews.Count > 0)
            {
                LayoutNodeViewColumn(
                    unreachableNodeViews,
                    currentX,
                    centerY + unreachableOffsetY,
                    nodeGap,
                    "Auto Layout Dialogue Graph");
            }

            CompleteLayoutOperation();
        }

        /// <summary>
        /// 将当前选中的节点按真实视图矩形左边缘对齐。
        /// </summary>
        public void AlignSelectedNodesLeft()
        {
            AlignSelectedNodesHorizontally(DialogueHorizontalAlignment.Left);
        }

        /// <summary>
        /// 将当前选中的节点按真实视图矩形水平中心对齐。
        /// </summary>
        public void AlignSelectedNodesCenter()
        {
            AlignSelectedNodesHorizontally(DialogueHorizontalAlignment.Center);
        }

        /// <summary>
        /// 将当前选中的节点按真实视图矩形右边缘对齐。
        /// </summary>
        public void AlignSelectedNodesRight()
        {
            AlignSelectedNodesHorizontally(DialogueHorizontalAlignment.Right);
        }

        private void AlignSelectedNodesTop()
        {
            AlignSelectedNodesVertically(DialogueVerticalAlignment.Top);
        }

        private void AlignSelectedNodesMiddle()
        {
            AlignSelectedNodesVertically(DialogueVerticalAlignment.Middle);
        }

        private void AlignSelectedNodesBottom()
        {
            AlignSelectedNodesVertically(DialogueVerticalAlignment.Bottom);
        }

        private void DistributeSelectedNodesHorizontally()
        {
            List<DialogueNodeView> selectedNodeViews = GetSelectedNodeViews();
            if (selectedNodeViews.Count < 3)
            {
                return;
            }

            List<DialogueNodeView> sortedNodeViews = selectedNodeViews
                .OrderBy(nodeView => nodeView.GetPosition().center.x)
                .ToList();

            float firstCenterX = sortedNodeViews[0].GetPosition().center.x;
            float lastCenterX = sortedNodeViews[^1].GetPosition().center.x;
            float spacing = (lastCenterX - firstCenterX) / (sortedNodeViews.Count - 1);

            for (int i = 1; i < sortedNodeViews.Count - 1; i++)
            {
                DialogueNodeView nodeView = sortedNodeViews[i];
                Rect rect = nodeView.GetPosition();
                rect.x = firstCenterX + spacing * i - rect.width * 0.5f;
                ApplyLayoutPosition(nodeView, rect, "Distribute Dialogue Nodes Horizontally");
            }

            CompleteLayoutOperation();
        }

        private void DistributeSelectedNodesVertically()
        {
            List<DialogueNodeView> selectedNodeViews = GetSelectedNodeViews();
            if (selectedNodeViews.Count < 3)
            {
                return;
            }

            List<DialogueNodeView> sortedNodeViews = selectedNodeViews
                .OrderBy(nodeView => nodeView.GetPosition().center.y)
                .ToList();

            float firstCenterY = sortedNodeViews[0].GetPosition().center.y;
            float lastCenterY = sortedNodeViews[^1].GetPosition().center.y;
            float spacing = (lastCenterY - firstCenterY) / (sortedNodeViews.Count - 1);

            for (int i = 1; i < sortedNodeViews.Count - 1; i++)
            {
                DialogueNodeView nodeView = sortedNodeViews[i];
                Rect rect = nodeView.GetPosition();
                rect.y = firstCenterY + spacing * i - rect.height * 0.5f;
                ApplyLayoutPosition(nodeView, rect, "Distribute Dialogue Nodes Vertically");
            }

            CompleteLayoutOperation();
        }

        private void AlignSelectedNodesHorizontally(DialogueHorizontalAlignment alignment)
        {
            List<DialogueNodeView> selectedNodeViews = GetSelectedNodeViews();
            if (selectedNodeViews.Count < 2)
            {
                return;
            }

            List<Rect> rects = selectedNodeViews.Select(nodeView => nodeView.GetPosition()).ToList();
            float minX = rects.Min(rect => rect.xMin);
            float maxX = rects.Max(rect => rect.xMax);
            float centerX = (minX + maxX) * 0.5f;

            foreach (DialogueNodeView nodeView in selectedNodeViews)
            {
                Rect rect = nodeView.GetPosition();
                rect.x = alignment switch
                {
                    DialogueHorizontalAlignment.Left => minX,
                    DialogueHorizontalAlignment.Center => centerX - rect.width * 0.5f,
                    DialogueHorizontalAlignment.Right => maxX - rect.width,
                    _ => rect.x
                };

                ApplyLayoutPosition(nodeView, rect, "Align Dialogue Nodes");
            }

            CompleteLayoutOperation();
        }

        private void AlignSelectedNodesVertically(DialogueVerticalAlignment alignment)
        {
            List<DialogueNodeView> selectedNodeViews = GetSelectedNodeViews();
            if (selectedNodeViews.Count < 2)
            {
                return;
            }

            List<Rect> rects = selectedNodeViews.Select(nodeView => nodeView.GetPosition()).ToList();
            float minY = rects.Min(rect => rect.yMin);
            float maxY = rects.Max(rect => rect.yMax);
            float centerY = (minY + maxY) * 0.5f;

            foreach (DialogueNodeView nodeView in selectedNodeViews)
            {
                Rect rect = nodeView.GetPosition();
                rect.y = alignment switch
                {
                    DialogueVerticalAlignment.Top => minY,
                    DialogueVerticalAlignment.Middle => centerY - rect.height * 0.5f,
                    DialogueVerticalAlignment.Bottom => maxY - rect.height,
                    _ => rect.y
                };

                ApplyLayoutPosition(nodeView, rect, "Align Dialogue Nodes");
            }

            CompleteLayoutOperation();
        }

        private void ApplyLayoutPosition(DialogueNodeView nodeView, Rect rect, string undoName)
        {
            if (nodeView == null)
            {
                return;
            }

            nodeView.SetPosition(rect);
            viewModel.SetNodePositionFromView(nodeView.DialogueNode, rect.position, undoName);
        }

        private void CompleteLayoutOperation()
        {
            RefreshNodeViews();
            RequestEdgeRefresh();
        }

        private float LayoutNodeViewColumn(
            IReadOnlyList<DialogueNodeView> nodeViews,
            float x,
            float centerY,
            float nodeGap,
            string undoName)
        {
            if (nodeViews.Count == 0)
            {
                return 0f;
            }

            float totalHeight = nodeViews.Sum(nodeView => nodeView.GetPosition().height) + nodeGap * (nodeViews.Count - 1);
            float y = centerY - totalHeight * 0.5f;
            float maxWidth = 0f;

            foreach (DialogueNodeView nodeView in nodeViews)
            {
                Rect rect = nodeView.GetPosition();
                rect.position = new Vector2(x, y);
                ApplyLayoutPosition(nodeView, rect, undoName);

                y += rect.height + nodeGap;
                maxWidth = Mathf.Max(maxWidth, rect.width);
            }

            return maxWidth;
        }

        private List<DialogueNodeView> GetSelectedNodeViews()
        {
            return selection
                .OfType<DialogueNodeView>()
                .Where(nodeView => nodeView.DialogueNode != null)
                .ToList();
        }

        private DropdownMenuAction.Status GetAlignMenuStatus(DropdownMenuAction action)
        {
            return GetSelectedNodeViews().Count >= 2
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled;
        }

        private DropdownMenuAction.Status GetDistributeMenuStatus(DropdownMenuAction action)
        {
            return GetSelectedNodeViews().Count >= 3
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled;
        }

        private enum DialogueHorizontalAlignment
        {
            Left,
            Center,
            Right
        }

        private enum DialogueVerticalAlignment
        {
            Top,
            Middle,
            Bottom
        }
        #endregion

        #region 节点与连线创建
        private DialogueNodeView CreateNodeView(DialogueNode node)
        {
            DialogueNodeView nodeView = new DialogueNodeView(node);
            return nodeView;
        }

        private void CreateNode(Type nodeType, Vector2 position)
        {
            viewModel.CreateNode(nodeType, position);
        }

        private void CreateEdges(DialogueNode node)
        {
            DialogueNodeView outputView = FindNodeView(node);
            if (outputView?.OutputPort == null)
            {
                return;
            }

            foreach (DialogueNode target in viewModel.GetTargetsFrom(node))
            {
                DialogueNodeView inputView = FindNodeView(target);
                if (inputView?.InputPort == null)
                {
                    continue;
                }

                Edge edge = outputView.OutputPort.ConnectTo(inputView.InputPort);
                AddElement(edge);
            }
        }
        #endregion

        #region GraphView 变更处理
        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (isPopulating)
            {
                return change;
            }

            List<GraphElement> elementsToRemove = change.elementsToRemove?.ToList();
            List<Edge> edgesToCreate = change.edgesToCreate?.ToList();
            List<GraphElement> movedElements = change.movedElements?.ToList();

            isHandlingGraphChange = true;
            try
            {
                if (elementsToRemove != null)
                {
                    foreach (GraphElement element in elementsToRemove)
                    {
                        switch (element)
                        {
                            case Edge edge:
                                DisconnectEdge(edge);
                                break;
                            case DialogueNodeView nodeView:
                                viewModel.DeleteNode(nodeView.DialogueNode, false);
                                break;
                        }
                    }
                }

                if (edgesToCreate != null)
                {
                    foreach (Edge edge in edgesToCreate)
                    {
                        ConnectEdge(edge);
                    }
                }

                if (movedElements != null)
                {
                    foreach (GraphElement element in movedElements)
                    {
                        if (element is DialogueNodeView nodeView)
                        {
                            viewModel.MoveNode(nodeView.DialogueNode, nodeView.GetPosition().position);
                        }
                    }
                }
            }
            finally
            {
                isHandlingGraphChange = false;
            }

            RefreshNodeViews();
            RequestEdgeRefresh();
            FlushPendingPopulate();
            return change;
        }
        #endregion

        #region 延迟刷新与重绘
        private void FlushPendingPopulate()
        {
            if (!pendingPopulate || isHandlingGraphChange)
            {
                return;
            }

            pendingPopulate = false;
            Populate();
        }

        private void RequestEdgeRefresh()
        {
            RefreshEdges();
            schedule.Execute(RefreshEdges);
        }

        private void RefreshEdges()
        {
            foreach (Edge edge in edges.ToList())
            {
                edge.UpdateEdgeControl();
                edge.MarkDirtyRepaint();
            }
        }
        #endregion

        #region 连线数据同步
        private void ConnectEdge(Edge edge)
        {
            if (TryGetNodes(edge, out DialogueNode outputNode, out DialogueNode inputNode))
            {
                RemoveConflictingLinearSpeechEdges(edge, outputNode, inputNode);
                RemoveConflictingChoiceOwnerEdges(edge, outputNode, inputNode);
                viewModel.Connect(outputNode, inputNode);
            }
        }

        private void DisconnectEdge(Edge edge)
        {
            if (TryGetNodes(edge, out DialogueNode outputNode, out DialogueNode inputNode))
            {
                viewModel.Disconnect(outputNode, inputNode);
            }
        }

        private static bool TryGetNodes(Edge edge, out DialogueNode outputNode, out DialogueNode inputNode)
        {
            outputNode = null;
            inputNode = null;

            if (edge?.output?.node is not DialogueNodeView outputNodeView ||
                edge.input?.node is not DialogueNodeView inputNodeView)
            {
                return false;
            }

            outputNode = outputNodeView.DialogueNode;
            inputNode = inputNodeView.DialogueNode;
            return true;
        }

        private void RemoveConflictingLinearSpeechEdges(Edge newEdge, DialogueNode outputNode, DialogueNode inputNode)
        {
            if (outputNode is not DialogueSpeechNode || inputNode is DialogueChoiceNode)
            {
                return;
            }

            foreach (Edge edge in edges.ToList())
            {
                if (edge == newEdge)
                {
                    continue;
                }

                if (!TryGetNodes(edge, out DialogueNode existingOutput, out DialogueNode existingInput))
                {
                    continue;
                }

                if (existingOutput == outputNode && existingInput is not DialogueChoiceNode)
                {
                    RemoveElement(edge);
                }
            }
        }

        private void RemoveConflictingChoiceOwnerEdges(Edge newEdge, DialogueNode outputNode, DialogueNode inputNode)
        {
            if (outputNode is not DialogueSpeechNode || inputNode is not DialogueChoiceNode)
            {
                return;
            }

            foreach (Edge edge in edges.ToList())
            {
                if (edge == newEdge)
                {
                    continue;
                }

                if (!TryGetNodes(edge, out DialogueNode existingOutput, out DialogueNode existingInput))
                {
                    continue;
                }

                if (existingOutput is DialogueSpeechNode && existingInput == inputNode)
                {
                    RemoveElement(edge);
                }
            }
        }

        private DialogueNodeView FindNodeView(DialogueNode node)
        {
            if (node == null)
            {
                return null;
            }

            return GetNodeByGuid(node.Guid) as DialogueNodeView;
        }
        #endregion

        #region Selection
        private void SyncSelectedNodeToViewModel()
        {
            DialogueNode selectedNode = selection
                .OfType<DialogueNodeView>()
                .Select(nodeView => nodeView.DialogueNode)
                .FirstOrDefault();

            viewModel.SelectNode(selectedNode);
        }
        #endregion
    }
}
