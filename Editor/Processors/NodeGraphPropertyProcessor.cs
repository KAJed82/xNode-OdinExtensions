using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using XNode;
using XNode.Odin;

namespace XNodeEditor.Odin
{
	public class NodeGraphPropertyProcessor<TNodeGraph> : OdinPropertyProcessor<TNodeGraph>
		where TNodeGraph : NodeGraph
	{
		protected const string FOLDBUTTON_GROUP = "xnode:foldall";

		public System.Action foldAll;
		public System.Action unfoldAll;

		protected override void Initialize()
		{
			base.Initialize();

			foldAll = FoldAll;
			unfoldAll = UnfoldAll;
		}

		public override void ProcessMemberProperties( List<InspectorPropertyInfo> infos )
		{
			if ( NodeEditor.InNodeEditor )
				return;

			if ( Property.GetAttribute<ShowFoldButtonsAttribute>() != null )
			{
				var unfoldAllProperty = InspectorPropertyInfo.CreateForDelegate(
					"unfoldAllNodes",
					0,
					typeof( TNodeGraph ),
					unfoldAll,
					new ButtonGroupAttribute( FOLDBUTTON_GROUP )
				);
				infos.Insert( 0, unfoldAllProperty );
				var foldAllProperty = InspectorPropertyInfo.CreateForDelegate(
					"foldAllNodes",
					0,
					typeof( TNodeGraph ),
					foldAll,
					new ButtonGroupAttribute( FOLDBUTTON_GROUP )
				);
				infos.Insert( 0, foldAllProperty );
			}
		}

		protected void FoldAll()
		{
			foreach ( var nodeGraph in Property.ValueEntry.WeakValues.OfType<TNodeGraph>() )
			{
				Undo.RegisterFullObjectHierarchyUndo( nodeGraph, "Fold Nodes" );
				foreach ( var node in nodeGraph.nodes )
				{
					Undo.RegisterFullObjectHierarchyUndo( node, "Fold Nodes" );

					bool foldable;
					node.GetType().TryGetAttributeFoldable( out foldable );

					if ( foldable )
						node.folded = true;
				}

				Undo.CollapseUndoOperations( Undo.GetCurrentGroup() );
			}

			// Find node editor windows and repaint them
			foreach ( var nodeEditorWindow in Resources.FindObjectsOfTypeAll<NodeEditorWindow>() )
			{
				// Twice because of port caching
				nodeEditorWindow.Repaint();
				nodeEditorWindow.Repaint();
			}
		}

		protected void UnfoldAll()
		{
			foreach ( var nodeGraph in Property.ValueEntry.WeakValues.OfType<TNodeGraph>() )
			{
				Undo.RegisterFullObjectHierarchyUndo( nodeGraph, "Unfold Nodes" );
				foreach ( var node in nodeGraph.nodes )
				{
					Undo.RegisterFullObjectHierarchyUndo( node, "Unfold Nodes" );
					node.folded = false;
				}

				Undo.CollapseUndoOperations( Undo.GetCurrentGroup() );
			}

			// Find node editor windows and repaint them
			foreach ( var nodeEditorWindow in Resources.FindObjectsOfTypeAll<NodeEditorWindow>() )
			{
				// Twice because of port caching
				nodeEditorWindow.Repaint();
				nodeEditorWindow.Repaint();
			}
		}
	}
}
