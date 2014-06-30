﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using Invert.uFrame;
using Invert.uFrame.Editor;
using Invert.uFrame.Editor.ElementDesigner;
using Invert.uFrame.Editor.ElementDesigner.Commands;
using UnityEditor;
using UnityEngine;

public class ElementDrawer : DiagramNodeDrawer<ElementData>
{
    public ElementDrawer()
    {
    }

    public override GUIStyle BackgroundStyle
    {
        get
        {
            if (Data.IsTemplate)
            {
                return UFStyles.DiagramBox4;
            }
            return UFStyles.DiagramBox3;
        }
    }

    public override GUIStyle ItemStyle
    {
        get { return UFStyles.Item4; }
    }

    public ElementDrawer(ElementData data, ElementsDiagram diagram)
        : base(data, diagram)
    {
    }



    public NodeItemHeader PropertiesHeader
    {
        get
        {
            if (_propertiesHeader == null)
            {
                _propertiesHeader = Container.Resolve<NodeItemHeader>();

                _propertiesHeader.Label = "Properties";
                _propertiesHeader.HeaderType = typeof (ViewModelPropertyData);
                _propertiesHeader.AddCommand = Container.Resolve<AddElementPropertyCommand>();
            }
            return _propertiesHeader;
        }
        set { _propertiesHeader = value; }
    }

    public NodeItemHeader CollectionsHeader
    {
        get
        {
            
             if (_collectionsHeader == null)
            {
                _collectionsHeader = Container.Resolve<NodeItemHeader>();
                _collectionsHeader.Label = "Collections";
                _collectionsHeader.HeaderType = typeof (ViewModelCollectionData);
                _collectionsHeader.AddCommand = Container.Resolve<AddElementCollectionCommand>();
            }
            return _collectionsHeader;
        }
        set { _collectionsHeader = value; }
    }

    public NodeItemHeader CommandsHeader
    {
        get
        {
            if (_commandsHeader == null)
            {
                _commandsHeader = Container.Resolve<NodeItemHeader>();
                _commandsHeader.Label = "Commands";
                _commandsHeader.HeaderType = typeof(ViewModelCommandData);
                _commandsHeader.AddCommand = Container.Resolve<AddElementCommandCommand>();
            }
            return _commandsHeader;
        }
        set { _commandsHeader = value; }
    }


    private NodeItemHeader _propertiesHeader;
    private NodeItemHeader _collectionsHeader;
    private NodeItemHeader _commandsHeader;

    private float _width;

    protected override GUIStyle GetHighlighter()
    {
        if (!Data.IsMultiInstance)
        {
            return UFStyles.BoxHighlighter4;
        }
        return base.GetHighlighter();
    }

    public override float Width
    {
        get
        {

            return Math.Max(110 * Scale, _width);
        }
    }

    public override void CalculateBounds()
    {
        base.CalculateBounds();
        _maxNameWidth = MaxNameWidth(EditorStyles.label);
        _maxTypeWidth = MaxTypeWidth(EditorStyles.label);


        _width = Math.Max(EditorStyles.largeLabel.CalcSize(new GUIContent(Data.FullLabel)).x + 50, _maxNameWidth + _maxTypeWidth);
    }

    private float _maxTypeWidth;
    private float _maxNameWidth;

    public virtual float MaxTypeWidth(GUIStyle style)
    {
        var maxLengthItem = Vector2.zero;
  
        if (AllowCollapsing && !Data.IsCollapsed)
        {
            foreach (var item in Data.ViewModelItems)
            {
                var rtn = item.RelatedTypeName ?? "[None]";

                if (ElementDataBase.TypeNameAliases.ContainsKey(rtn))
                {
                    rtn = ElementDataBase.TypeNameAliases[rtn];
                }
                var newSize = style.CalcSize(new GUIContent(ElementDataBase.TypeAlias(rtn)));

                if (maxLengthItem.x < newSize.x)
                {
                    maxLengthItem = newSize;
                }
            }
        }
        return maxLengthItem.x + 8;

    }
    public float MaxNameWidth(GUIStyle style)
    {
        //style.fontStyle= FontStyle.Bold;
        var maxLengthItem = Vector2.zero;
        if (AllowCollapsing && !Data.IsCollapsed)
        {
            foreach (var item in Data.ViewModelItems)
            {
                var newSize = style.CalcSize(new GUIContent(item.Name));

                if (maxLengthItem.x < newSize.x)
                {
                    maxLengthItem = newSize;
                }
            }
        }


        return maxLengthItem.x + 8;

    }
    protected override void DrawSelectedItem(IDiagramNodeItem nodeItem, ElementsDiagram diagram)
    {
        var item = nodeItem as IViewModelItem;
        if (item == null)
        {
            base.DrawSelectedItem(nodeItem, diagram);
            return;
        }
        GUILayout.Space(7);
        var rtn = item.RelatedTypeName ?? "[None]";

        if (ElementDataBase.TypeNameAliases.ContainsKey(rtn))
        {
            rtn = ElementDataBase.TypeNameAliases[rtn];
        }
        if (GUILayout.Button(rtn, UFStyles.ClearItemStyle))
        {
            var commandName = item.GetType().Name.Replace("Data", "") + "TypeSelection";
            var command = Container.Resolve<IEditorCommand>(commandName);
            if (command == null)
            {
                Debug.Log("Type selection command not found for " + commandName);
            }
            else
            {
                Diagram.ExecuteCommand(command);
            }
         

         
        }
        base.DrawSelectedItem(nodeItem, diagram);
    }

    protected override void DrawItemLabel(IDiagramNodeItem item)
    {
        var vmItem = item as IViewModelItem;
        if (vmItem == null)
        {
            base.DrawItemLabel(item);
        }
        else
        {
            GUILayout.BeginArea(item.Position.Scale(Scale));
            GUILayout.BeginHorizontal();
            GUILayout.Space(7);

            var style = new GUIStyle(UFStyles.ClearItemStyle)
            {
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                normal = {textColor = BackgroundStyle.normal.textColor}
            };
            // style.fontSize = Mathf.RoundToInt(style.fontSize * Scale);
            var rtn = vmItem.RelatedTypeName ?? string.Empty;
            if (ElementDataBase.TypeNameAliases.ContainsKey(rtn))
            {
                rtn = ElementDataBase.TypeNameAliases[rtn];
            }
            //GUILayout.Label((_maxTypeWidth * Scale).ToString(), style, GUILayout.Width(_maxTypeWidth * Scale));
            GUILayout.Label(rtn, style, GUILayout.Width(_maxTypeWidth * Scale));
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleLeft;
            //GUILayout.Label((_maxNameWidth * Scale).ToString(), style, GUILayout.Width(_maxNameWidth * Scale));
            GUILayout.Label(vmItem.Name, style, GUILayout.Width(_maxNameWidth * Scale));
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
        //base.DrawItemLabel(item);

    }

    protected override IEnumerable<DiagramSubItemGroup> GetItemGroups()
    {
        var elementData = Data as ElementData;
        if (elementData != null && Diagram.Data.CurrentFilter == Data)
        {
            //yield return new DiagramSubItemGroup()
            //{
            //    Header = ViewsHeader,
            //    Items = elementData.IncludedViews.Cast<IDiagramSubItem>().ToArray()
            //};
            //yield return new DiagramSubItemGroup()
            //{
            //    Header = ComponentsHeader,
            //    Items = elementData.IncludedComponents.Cast<IDiagramSubItem>().ToArray()
            //};


        }
        yield return new DiagramSubItemGroup()
        {
            Header = PropertiesHeader,
            Items = Data.Properties.Cast<IDiagramNodeItem>().ToArray()
        };
        yield return new DiagramSubItemGroup()
        {
            Header = CollectionsHeader,
            Items = Data.Collections.Cast<IDiagramNodeItem>().ToArray()
        };
        yield return new DiagramSubItemGroup()
        {
            Header = CommandsHeader,
            Items = Data.Commands.Cast<IDiagramNodeItem>().ToArray()
        };
    }
}